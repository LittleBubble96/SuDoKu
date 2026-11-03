#if UNITY_EDITOR && !TE_RELEASE
using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using GameLogic;
using GameLogic.Data;
using GameLogic.Sudoku;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace SudokuEditor
{
    [InitializeOnLoad]
    public static class SudokuSignInValidation
    {
        private const string Pending="Sudoku.SignIn.Pending", Phase="Sudoku.SignIn.Phase",
            Output="Sudoku.SignIn.Output", Cache="Sudoku.SignIn.Cache", HadCache="Sudoku.SignIn.HadCache",
            Result="Sudoku.SignIn.Result";
        private const string Dialog="Panel/Dialog/";
        static SudokuSignInValidation() { EditorApplication.update += Tick; }
        [MenuItem("Tools/数独/验证每日签到")]
        public static void Run() => RunToDirectory(Path.GetFullPath("Library/SudokuSignInValidation"));

        public static void RunToDirectory(string directory)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("请先退出播放模式。");
            Directory.CreateDirectory(directory);
            var path=Path.Combine(Application.persistentDataPath,"game_cache.json");
            SessionState.SetString(Output,directory); SessionState.SetString(Cache,path);
            SessionState.SetBool(HadCache,File.Exists(path));
            var data=new GameCacheData();
            if(File.Exists(path))
            {
                File.Copy(path,Path.Combine(directory,"cache-before-test.json"),true);
                JsonUtility.FromJsonOverwrite(File.ReadAllText(path),data);
            }
            data.signInDays=0; data.signInDate=null;
            File.WriteAllText(path,JsonUtility.ToJson(data));
            SessionState.SetBool(Pending,true); SessionState.SetInt(Phase,0);
            SudokuDemoTools.PlayForValidation();
        }
        private static void Tick()
        {
            if(!SessionState.GetBool(Pending,false)) return;
            int phase=SessionState.GetInt(Phase,0);
            if(!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Finish(phase==2 ? SessionState.GetString(Result,"No result") : "FAIL: validation stopped early.");
                return;
            }
            if(phase!=0 || !EditorApplication.isPlaying || SudokuPresenter.Current==null) return;
            SessionState.SetInt(Phase,1); ValidateAsync().Forget();
        }
        private static async UniTaskVoid ValidateAsync()
        {
            string result;
            using(var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(60)))
            {
                try
                {
                    Application.runInBackground=true;
                    var token=timeout.Token;
                    var home=await GameModule.UI.GetUIAsyncAwait<UIHome>(token);
                    await home.WaitForContentAsync(token);
                    var popup=await WaitForPopup(token);
                    Require(!SudokuPresenter.DailySignIn.ClaimedToday,"home auto opens unclaimed day");
                    var layout=popup.FindChild(Dialog+"Layout");
                    Require(layout.childCount==8 && !layout.Find("Item").gameObject.activeSelf,"hidden template and seven items");
                    var template=(RectTransform)layout.Find("Item");
                    var first=(RectTransform)layout.Find("Item1");
                    for(int i=0;i<7;i++)
                    {
                        var item=(RectTransform)layout.Find("Item"+(i+1));
                        float width=i==6 ? template.sizeDelta.x*2+10 : template.sizeDelta.x;
                        Require(Mathf.Abs(item.sizeDelta.x-width)<.01f && Mathf.Abs(item.sizeDelta.y-template.sizeDelta.y)<.01f,"item dimensions");
                        var position=template.anchoredPosition+new Vector2(i%4*(template.sizeDelta.x+10)+(width-template.sizeDelta.x)*.5f,
                            -(i/4)*template.sizeDelta.y);
                        Require(Vector2.Distance(item.anchoredPosition,position)<.01f,"horizontal 10 / vertical 0");
                    }
                    Require(layout.Find("Item4/bg/Reward/m_num").GetComponent<TMP_Text>().text=="× 1" &&
                        layout.Find("Item7/bg/Reward/m_num").GetComponent<TMP_Text>().text=="× 2","prop quantities");
                    await UniTask.Delay(100,ignoreTimeScale:true,cancellationToken:token);
                    SudokuDemoValidation.CaptureCanvas(popup.gameObject,Path.Combine(OutputPath,"sign-in-day1.png"));
                    var data=SudokuPresenter.Cache.CacheData;
                    int coins=data.gamePropData.coinCount;
                    Click(popup,"DirectClaimBtn");
                    await UniTask.WaitUntil(()=>!popup.IsOpen,cancellationToken:token);
                    Require(!popup.IsOpen && data.signInDays==1 && data.gamePropData.coinCount==coins+20,"direct claim");
                    int current=data.gamePropData.coinCount;
                    await SudokuPresenter.ShowDailySignInAsync(home);
                    popup=await WaitForPopup(token);
                    popup.FindChildComponent<Button>("Panel/Close_Btn").onClick.Invoke();
                    await UniTask.WaitUntil(()=>!popup.IsOpen,cancellationToken:token);
                    Require(data.signInDays==1 && data.gamePropData.coinCount==current,"same-day reopen cannot duplicate");
                    await CheckPath(home,"Bg",false,token);
                    await CheckPath(home,"CloseBtn",false,token);
                    await CheckPath(home,"Layout/Item1/bg/btn",false,token);
                    await CheckPath(home,"AdClaimBtn",true,token);
                    data.signInDays=3; data.signInDate=null;
                    int hints=data.gamePropData.tipCount;
                    await SudokuPresenter.ShowDailySignInAsync(home); popup=await WaitForPopup(token);
                    Click(popup,"AdClaimBtn");
                    await UniTask.WaitUntil(()=>!popup.IsOpen && !SudokuPresenter.Current.IsAdShowing,cancellationToken:token);
                    Require(data.gamePropData.tipCount==hints+2 && data.signInDays==4,"advertisement doubles hint day");
                    await SudokuPresenter.Cache.FlushAsync();
                    var reload=new GameCacheManager(); await reload.LoadAsync();
                    Require(reload.CacheData.signInDays==4 && reload.CacheData.signInDate==data.signInDate &&
                        reload.CacheData.gamePropData.tipCount==data.gamePropData.tipCount,"native sign-in cache roundtrip");

                    await UniTask.Delay(1100,ignoreTimeScale:true,cancellationToken:token);
                    Vector3 origin=home.FindChild("Panel/Content/UIHome_Home/Top/CoinRoot/Btn").position;
                    var coinPlay=RewardEffectPlayer.PlayCoinAsync(50,origin);
                    await UniTask.WaitUntil(()=>GameObject.Find("RewardEffect")!=null,cancellationToken:token);
                    await UniTask.Yield(token);
                    var effect=GameObject.Find("RewardEffect");
                    Require(effect.transform.Find("Coin/m_num").GetComponent<TMP_Text>().text=="50" &&
                        effect.transform.Find("Coin").gameObject.activeSelf && !effect.transform.Find("Prop").gameObject.activeSelf,"coin effect / amount");
                    Require(!effect.GetComponent<CanvasGroup>().blocksRaycasts,"effect does not block clicks");
                    await coinPlay;
                    await UniTask.Yield(token);
                    var propPlay=RewardEffectPlayer.PlayPropAsync(2,origin);
                    await UniTask.WaitUntil(()=>GameObject.Find("RewardEffect")!=null,cancellationToken:token);
                    await UniTask.Yield(token);
                    effect=GameObject.Find("RewardEffect");
                    Require(effect.transform.Find("Prop/m_num").GetComponent<TMP_Text>().text=="× 2" &&
                        !effect.transform.Find("Coin").gameObject.activeSelf && effect.transform.Find("Prop").gameObject.activeSelf,"prop effect / amount");
                    await propPlay;
                    GameModule.UI.DestroyUI<UIDailySignIn>();
                    result="PASS: home automatic popup, local 7-item layout, exact gaps / day7 width, direct/background/close/today-card claims, no repeated claims, Editor ad double coins/props, native cache and reward effects.";
                }
                catch(Exception error) { result=error.ToString(); }
            }
            SessionState.SetString(Result,result); SessionState.SetInt(Phase,2);
            EditorApplication.isPlaying=false;
        }
        private static async UniTask CheckPath(UIHome home,string path,bool doubled,CancellationToken token)
        {
            var data=SudokuPresenter.Cache.CacheData;
            data.signInDays=0; data.signInDate=null;
            int coins=data.gamePropData.coinCount;
            await SudokuPresenter.ShowDailySignInAsync(home);
            var popup=await WaitForPopup(token);
            Click(popup,path);
            await UniTask.WaitUntil(()=>!popup.IsOpen && !SudokuPresenter.Current.IsAdShowing,cancellationToken:token);
            Require(data.signInDays==1 && data.gamePropData.coinCount==coins+(doubled?40:20),"claim path "+path);
            await UniTask.Delay(1100,ignoreTimeScale:true,cancellationToken:token);
        }
        private static async UniTask<UIDailySignIn> WaitForPopup(CancellationToken token)
        {
            UIDailySignIn popup;
            while((popup=await GameModule.UI.GetUIAsyncAwait<UIDailySignIn>(token))==null) await UniTask.Yield(token);
            await UniTask.WaitUntil(()=>popup.IsOpen && popup.IsPrepare && !popup.IsAnimating && !UIInteractionLock.IsLocked,cancellationToken:token);
            return popup;
        }
        private static string OutputPath=>SessionState.GetString(Output,"");
        private static void Click(UIDailySignIn popup,string path)=>popup.FindChildComponent<Button>(Dialog+path).onClick.Invoke();
        private static void Require(bool value,string name) { if(!value) throw new Exception("FAIL: "+name); }
        private static void Finish(string result)
        {
            var path=SessionState.GetString(Cache,"");
            if(SessionState.GetBool(HadCache,false)) File.Copy(Path.Combine(OutputPath,"cache-before-test.json"),path,true);
            else if(File.Exists(path)) File.Delete(path);
            File.WriteAllText(Path.Combine(OutputPath,"signin-validation.txt"),result+"\n"+DateTime.Now.ToString("s"));
            SessionState.EraseBool(Pending);
            if(result.StartsWith("PASS")) Debug.Log(result); else Debug.LogError(result);
        }
    }
}
#endif
