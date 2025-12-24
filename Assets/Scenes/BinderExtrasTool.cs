using System;
using UnityEngine;

public class BinderExtrasTool : MonoBehaviour
{
    private const string JunkKey = "BINDER_TEST_JUNK";
    private const int WarnBytes = 500_000;
    private const int DangerBytes = 800_000;

    private static AndroidJavaClass UnityPlayerClass;
    private static AndroidJavaClass ParcelClass;

    // -------- Action 1: Inject --------

    public static void InjectBytes(int payloadBytes)
    {
        if (Application.platform != RuntimePlatform.Android) return;
        if (payloadBytes <= 0) return;

        try
        {
            AndroidJavaObject intent = GetCurrentIntent();
            if (intent == null) return;

            byte[] junk = new byte[payloadBytes];
            for (int i = 0; i < junk.Length; i++) junk[i] = (byte)(i & 0xFF);

            intent.Call("putExtra", JunkKey, junk);

            Debug.Log($"[BinderExtrasTool] InjectBytes: {payloadBytes} bytes into Intent extra '{JunkKey}'.");
        }
        catch (Exception e)
        {
            Debug.LogError("[BinderExtrasTool] InjectBytes error: " + e);
        }
    }

    public static void InjectString(int charCount)
    {
        if (Application.platform != RuntimePlatform.Android) return;
        if (charCount <= 0) return;

        try
        {
            AndroidJavaObject intent = GetCurrentIntent();
            if (intent == null) return;

            string junk = new string('x', charCount);
            intent.Call("putExtra", JunkKey, junk);

            Debug.Log($"[BinderExtrasTool] InjectString: {charCount} chars into Intent extra '{JunkKey}'.");
        }
        catch (Exception e)
        {
            Debug.LogError("[BinderExtrasTool] InjectString error: " + e);
        }
    }

    public static void ClearInjected()
    {
        if (Application.platform != RuntimePlatform.Android) return;

        try
        {
            AndroidJavaObject intent = GetCurrentIntent();
            if (intent == null) return;

            intent.Call("removeExtra", JunkKey);

            Debug.Log($"[BinderExtrasTool] ClearInjected: removed Intent extra '{JunkKey}'.");
        }
        catch (Exception e)
        {
            Debug.LogError("[BinderExtrasTool] ClearInjected error: " + e);
        }
    }

    // -------- Action 2: Measure --------
    public static void MeasureExtrasSizeBytes()
    {
        if (Application.platform != RuntimePlatform.Android) return;

        try
        {
            AndroidJavaObject intent = GetCurrentIntent();
            if (intent == null) return;

            AndroidJavaObject extras = intent.Call<AndroidJavaObject>("getExtras");
            if (extras == null)
            {
                Debug.Log("[BinderExtrasTool] Measure: Extras is null (Size ~ 0).");
                return;
            }

            ParcelClass ??= new AndroidJavaClass("android.os.Parcel");

            AndroidJavaObject parcel = null;
            try
            {
                parcel = ParcelClass.CallStatic<AndroidJavaObject>("obtain");
                extras.Call("writeToParcel", parcel, 0);

                int sizeBytes = parcel.Call<int>("dataSize");
                LogMeasurement(sizeBytes);
            }
            finally
            {
                if (parcel != null) parcel.Call("recycle");
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[BinderExtrasTool] MeasureExtrasSizeBytes error: " + e);
            return;
        }
    }

    // -------- Internals --------

    private static AndroidJavaObject GetCurrentIntent()
    {
        UnityPlayerClass ??= new AndroidJavaClass("com.unity3d.player.UnityPlayer");

        AndroidJavaObject activity = UnityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
        if (activity == null)
        {
            Debug.LogError("[BinderExtrasTool] currentActivity is null.");
            return null;
        }

        AndroidJavaObject intent = activity.Call<AndroidJavaObject>("getIntent");
        if (intent == null) Debug.LogError("[BinderExtrasTool] getIntent() returned null.");

        return intent;
    }

    private static void LogMeasurement(int sizeBytes)
    {
        int sizeKb = sizeBytes / 1024;
        Debug.Log($"[BinderExtrasTool] Measure: Intent Extras Size = {sizeBytes} bytes (~{sizeKb} KB)");

        if (sizeBytes >= DangerBytes) Debug.LogError("[BinderExtrasTool] DANGER: Extras is extremely large. High risk of TransactionTooLargeException.");
        else if (sizeBytes >= WarnBytes) Debug.LogWarning("[BinderExtrasTool] WARNING: Extras is large. Investigate oversized data sources.");
    }
}
