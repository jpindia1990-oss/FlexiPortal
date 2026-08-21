#if ANDROID
using Android.App;
using Android.Content;

namespace FlexiPortal.Mobile.Platforms.Android;

[BroadcastReceiver(Enabled = true, Exported = true)]
[IntentFilter(new[] { Intent.ActionBootCompleted, Intent.ActionMyPackageReplaced, "android.intent.action.QUICKBOOT_POWERON" })]
public class BootReceiver : BroadcastReceiver
{
    public override void OnReceive(Context context, Intent intent)
    {
        if (intent.Action == Intent.ActionBootCompleted || intent.Action == Intent.ActionMyPackageReplaced)
        {
            try
            {
                var serviceIntent = new Intent(context, typeof(BackgroundLocationService));
                if (global::Android.OS.Build.VERSION.SdkInt >= global::Android.OS.BuildVersionCodes.O)
                    context.StartForegroundService(serviceIntent);
                else
                    context.StartService(serviceIntent);
            }
            catch (Exception ex)
            {
                global::Android.Util.Log.Error("BootReceiver", $"Boot start failed: {ex.Message}");
            }
        }
    }
}
#endif