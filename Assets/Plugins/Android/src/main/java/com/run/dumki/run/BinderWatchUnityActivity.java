package com.run.dumki.run;

import android.content.Intent;
import android.os.Bundle;
import android.os.Parcel;
import android.util.Log;

import com.unity3d.player.UnityPlayerActivity;

public class BinderWatchUnityActivity extends UnityPlayerActivity
{
    private static final String TAG = "BinderWatch";
    private static final int WARN_BYTES = 500_000;
    private static final int DANGER_BYTES = 800_000;

    @Override
    public void startActivity(Intent intent)
    {
        logIntentExtras(intent, "startActivity");
        super.startActivity(intent);
    }

    @Override
    public void startActivityForResult(Intent intent, int requestCode)
    {
        logIntentExtras(intent, "startActivityForResult");
        super.startActivityForResult(intent, requestCode);
    }

    private void logIntentExtras(Intent intent, String api)
    {
        if (intent == null) return;

        String cmp = (intent.getComponent() != null)
                ? intent.getComponent().flattenToShortString()
                : "null";

        Bundle extras = intent.getExtras();
        if (extras == null)
        {
            Log.d(TAG, api + " cmp=" + cmp + " extras=0");
            return;
        }

        Parcel p = null;
        try
        {
            p = Parcel.obtain();
            extras.writeToParcel(p, 0);
            int bytes = p.dataSize();

            String level =
                    (bytes >= DANGER_BYTES) ? "DANGER" :
                    (bytes >= WARN_BYTES)   ? "WARN"   : "OK";

            Log.e(TAG, api + " [" + level + "] cmp=" + cmp +
                    " extrasBytes=" + bytes + " (~" + (bytes / 1024) + "KB)");

            if (cmp.contains("com.google.android.gms.ads.AdActivity"))
                logExtrasBreakdown(extras, api, cmp);
        }
        catch (Throwable t)
        {
            Log.e(TAG, "measure error", t);
        }
        finally
        {
            if (p != null) p.recycle();
        }
    }

    // ---- Breakdown (same as before) ----

    private static void logExtrasBreakdown(Bundle extras, String api, String cmp)
    {
        if (extras == null) return;

        java.util.ArrayList<Item> items = new java.util.ArrayList<>();

        for (String key : extras.keySet())
        {
            Object value = extras.get(key);
            int bytes = measureSingleEntryBytes(key, value);

            String type = (value != null) ? value.getClass().getName() : "null";
            items.add(new Item(key, type, bytes));
        }

        java.util.Collections.sort(items, (a, b) -> Integer.compare(b.bytes, a.bytes));

        int total = 0;
        for (Item it : items) if (it.bytes > 0) total += it.bytes;

        Log.e(TAG, api + " breakdown cmp=" + cmp + " keys=" + items.size() +
                " approxSumBytes=" + total + " (~" + (total / 1024) + "KB)");

        int top = Math.min(10, items.size());
        for (int i = 0; i < top; i++)
        {
            Item it = items.get(i);
            Log.e(TAG, "  #" + (i + 1) + " key=" + it.key +
                    " type=" + it.type +
                    " bytes=" + it.bytes + " (~" + (it.bytes / 1024) + "KB)");
        }
    }

    private static int measureSingleEntryBytes(String key, Object value)
    {
        Parcel p = null;
        try
        {
            Bundle one = new Bundle();

            if (value == null) one.putString(key, null);
            else if (value instanceof String) one.putString(key, (String)value);
            else if (value instanceof Integer) one.putInt(key, (Integer)value);
            else if (value instanceof Long) one.putLong(key, (Long)value);
            else if (value instanceof Boolean) one.putBoolean(key, (Boolean)value);
            else if (value instanceof byte[]) one.putByteArray(key, (byte[])value);
            else if (value instanceof String[]) one.putStringArray(key, (String[])value);
            else if (value instanceof android.os.Parcelable) one.putParcelable(key, (android.os.Parcelable)value);
            else if (value instanceof java.io.Serializable) one.putSerializable(key, (java.io.Serializable)value);
            else one.putString(key, String.valueOf(value));

            p = Parcel.obtain();
            one.writeToParcel(p, 0);
            return p.dataSize();
        }
        catch (Throwable t)
        {
            return -1;
        }
        finally
        {
            if (p != null) p.recycle();
        }
    }

    private static final class Item
    {
        public final String key;
        public final String type;
        public final int bytes;

        public Item(String key, String type, int bytes)
        {
            this.key = key;
            this.type = type;
            this.bytes = bytes;
        }
    }
}
