using System;
using System.Collections;
using TMPro;
using UnityEngine;

public sealed class TestAds : MonoBehaviour
{
    private enum State
    {
        None,
        WaitingForSdk,
        ReadyToLoad,
        Loading,
        ReadyToShow,
        Showing,
        WaitingRetry
    }

    private const float SdkWarmupSeconds = 3f;
    private const string InterstitialAdUnitId = "d8d07eb9c12e7f99";

    [SerializeField] private TMP_Text _log;

    private State _state;
    private int _retryAttempt;
    private Coroutine _retryCoroutine;

    private void OnEnable() => RegisterCallbacks();
    private void OnDisable()
    {
        UnregisterCallbacks();
        CancelRetry();
    }

    private IEnumerator Start()
    {
        _state = State.WaitingForSdk;
        MaxSdk.InitializeSdk();

        yield return new WaitUntil(() => _state != State.WaitingForSdk);
        yield return new WaitForSeconds(SdkWarmupSeconds);

        _state = State.ReadyToLoad;
    }

    private void Update()
    {
        _log.text = $"State: {_state}\nRetry Attempt: {_retryAttempt}";
    }

    public void LoadInterstitial()
    {
        // Block manual load while waiting retry, loading, showing, etc.
        if (_state != State.ReadyToLoad) return;
        if (string.IsNullOrEmpty(InterstitialAdUnitId)) return;

        CancelRetry(); // safety
        _state = State.Loading;
        MaxSdk.LoadInterstitial(InterstitialAdUnitId);
    }

    public void ShowInterstitial()
    {
        if (_state != State.ReadyToShow) return;

        _state = State.Showing;
        MaxSdk.ShowInterstitial(InterstitialAdUnitId);
    }

    private void OnSdkInitialized(MaxSdkBase.SdkConfiguration _)
    {
        if (_state == State.WaitingForSdk) _state = State.None;
    }

    private void OnInterstitialLoaded(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        ThreadDispatcher.ExecuteOnMainThread(() =>
        {
            if (adUnitId != InterstitialAdUnitId) return;

            Debug.Log("Interstitial Loaded");
            CancelRetry();
            _retryAttempt = 0;
            _state = State.ReadyToShow;
        });
    }

    private void OnInterstitialLoadFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        ThreadDispatcher.ExecuteOnMainThread(() =>
        {
            if (adUnitId != InterstitialAdUnitId) return;

            Debug.LogWarning($"Interstitial Load Failed. Code={errorInfo.Code}, Message={errorInfo.Message}");

            _retryAttempt++;
            float delay = Mathf.Min(2f * _retryAttempt, 10f);

            _state = State.WaitingRetry;
            CancelRetry();
            _retryCoroutine = StartCoroutine(RetryAfter(delay));
        });
    }

    private IEnumerator RetryAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);

        if (!isActiveAndEnabled) yield break;
        if (_state != State.WaitingRetry) yield break;

        _state = State.ReadyToLoad;
        LoadInterstitial();
    }

    private void OnInterstitialHidden(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        ThreadDispatcher.ExecuteOnMainThread(() =>
        {
            if (adUnitId != InterstitialAdUnitId) return;

            Debug.Log("Interstitial Closed");
            _state = State.ReadyToLoad;
        });
    }

    private void OnInterstitialDisplayFailed(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        ThreadDispatcher.ExecuteOnMainThread(() =>
        {
            if (adUnitId != InterstitialAdUnitId) return;

            Debug.LogWarning($"Interstitial Display Failed. Code={errorInfo.Code}, Message={errorInfo.Message}");
            _state = State.ReadyToLoad;
        });
    }

    private void CancelRetry()
    {
        if (_retryCoroutine == null) return;
        StopCoroutine(_retryCoroutine);
        _retryCoroutine = null;
    }

    private void RegisterCallbacks()
    {
        MaxSdkCallbacks.OnSdkInitializedEvent += OnSdkInitialized;
        MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoaded;
        MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailed;
        MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialDisplayFailed;
        MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialHidden;
    }

    private void UnregisterCallbacks()
    {
        MaxSdkCallbacks.OnSdkInitializedEvent -= OnSdkInitialized;
        MaxSdkCallbacks.Interstitial.OnAdLoadedEvent -= OnInterstitialLoaded;
        MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent -= OnInterstitialLoadFailed;
        MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent -= OnInterstitialDisplayFailed;
        MaxSdkCallbacks.Interstitial.OnAdHiddenEvent -= OnInterstitialHidden;
    }

    public void DebugMax()
    {
        MaxSdk.ShowMediationDebugger();
    }
}
