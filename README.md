# TestTransactionException

A minimal Unity project to **measure** (and optionally reproduce) Android `TransactionTooLargeException` related to **Google Mobile Ads `AdActivity`** being launched with **large Intent extras**.

This repository focuses on **evidence collection**:
- Hooking `startActivity()` / `startActivityForResult()` at the Unity launcher Activity level.
- Measuring `Intent.getExtras()` size by writing the Bundle into a `Parcel`.
- Printing a **key-by-key breakdown** to identify which extras key dominates the payload.

> Note: The crash is **device-dependent**. On some devices, large Binder transactions fail earlier than others.  
> Even if the test project does not crash, demonstrating **oversized extras** is sufficient to confirm the root cause.

---

## Background

Some devices crash when showing ads with:

- `android.os.TransactionTooLargeException`
- `!!! FAILED BINDER TRANSACTION !!!`

The log typically mentions launching: com.google.android.gms.ads.AdActivity

This indicates the failure happens at the Android Binder IPC boundary during Activity launch.

---

## What this project measures

When `AdActivity` is launched, the Google Mobile Ads SDK passes extras to the activity.  
This project captures that `Intent` and measures:

- `extrasBytes` (size of the Bundle when written to a `Parcel`)
- Top keys contributing to size (dominant key usually: `AdOverlayInfo`)

In real-world cases, nearly all bytes come from:

- `com.google.android.gms.ads.internal.overlay.AdOverlayInfo` (type: `android.os.Bundle`)

---

## Repro scope: Production vs Test

### Production project (observed crash on some devices)
Typical measured values (example):
- `AdActivity` extras: ~528 KB
- Parcel size: ~543 KB
- Dominant key: `AdOverlayInfo` (~99% of bytes)
- Result: **crash on specific devices** (device-dependent threshold)
  
  ![image alt](https://github.com/Gemity/TestTransactionException/blob/main/adactivity_extras_breakdown_prod.png)

### This test project
Typical measured values:
- `AdActivity` extras: ~342 KB (varies)
- Dominant key: `AdOverlayInfo`
- Result: **usually no crash** (still proves oversized extras exist)
  
  ![image alt](https://github.com/Gemity/TestTransactionException/blob/main/adactivity_extras_breakdown_test.png)
---

## Devices where crash was observed (production)

Examples:
- Tecno Pova 3
- Vivo Y17s
- Google Pixel 4

> The exact crash threshold varies by device / OS / ROM.

---

## How it works (implementation overview)

### 1) Custom Unity Launcher Activity (Java)
We replace the default launcher activity with a custom activity that extends:

- `UnityPlayerActivity` (for a minimal test project)
- or `com.google.firebase.MessagingUnityPlayerActivity` (if the project includes Firebase Messaging)

The activity overrides:

- `startActivity(Intent intent)`
- `startActivityForResult(Intent intent, int requestCode)`

Then it:
- Reads `intent.getExtras()`
- Writes Bundle to `Parcel`
- Uses `Parcel.dataSize()` to measure bytes
- Dumps top keys and sizes

### 2) AndroidManifest launcher mapping
`AndroidManifest.xml` is configured so the launcher activity is:

- `com.run.dumki.run.BinderWatchUnityActivity`

---

## Project structure
Assets/
Plugins/
Android/
AndroidManifest.xml
src/
main/
java/
com/
run/
dumki/
run/
BinderWatchUnityActivity.java

---

## Build & Run instructions

1. Open in Unity **2022.3.x** (this repo was tested with Unity 2022.3 LTS).
2. Switch platform to **Android**.
3. Build and run on a device.
4. Trigger an ad show flow (Interstitial / Rewarded) to cause `AdActivity` launch.
5. Observe Logcat output.
---

## Expected Logcat output

Filter by tag:

- `BinderWatch`

Example (OK/WARN/DANGER):
- BinderWatch: startActivity [OK] cmp=.../com.google.android.gms.ads.AdActivity extrasBytes=350360 (~342KB)
- BinderWatch: startActivity breakdown cmp=.../com.google.android.gms.ads.AdActivity keys=3 approxSumBytes=350384 (~342KB)
- BinderWatch: #1 key=com.google.android.gms.ads.internal.overlay.AdOverlayInfo type=android.os.Bundle bytes=350168 (~341KB)
- BinderWatch: #2 key=com.google.android.gms.ads.internal.overlay.useClientJar type=java.lang.Boolean bytes=40 (~0KB)
- BinderWatch: #3 key=showCloseOnOverlayOpened type=java.lang.Boolean bytes=76 (~0KB)
- TransactionTooLargeException
!!! FAILED BINDER TRANSACTION !!! (parcel size = ...)
- Transaction too large, intent: Intent { cmp=.../com.google.android.gms.ads.AdActivity (has extras) }


---

## Notes about mediation / SDK versions (important)

- In production, the app is using **AppLovin MAX mediation**.
- AdMob is used **as a mediated network** (not the standalone Google Mobile Ads Unity plugin directly).
- The `AdActivity` and `AdOverlayInfo` extras are created by the **Google Mobile Ads Android SDK**.
- The test project is intended to isolate and measure the `AdActivity` extras behavior.

If you are comparing against Google Mobile Ads Unity plugin versions:
- Google Mobile Ads Unity Plugin versions may be tested against specific Android SDK versions.
- In mediation scenarios, the effective Google Mobile Ads Android SDK version is determined by the mediation adapter dependency graph.

---

## Why "bidding should only be a few KB" is not sufficient here

Even if a bidding token is small, the actual extras passed to `AdActivity` can be much larger due to internal SDK overlay bundles (`AdOverlayInfo`).  
This project confirms size by **measuring marshaled Parcel bytes**, not by assuming string/encoding sizes.

---




