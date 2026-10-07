package com.pertamina.smartsheep_mobile

import android.app.NotificationChannel
import android.app.NotificationManager
import android.os.Build
import android.os.Bundle
import io.flutter.embedding.android.FlutterActivity

class MainActivity : FlutterActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        createNotificationChannel()
    }

    // Push notifications use this channel (the API sets it as ChannelId, and
    // the manifest makes it the FCM default). High importance is what lets a
    // reminder appear as a heads-up banner rather than a silent tray icon.
    private fun createNotificationChannel() {
        if (Build.VERSION.SDK_INT < Build.VERSION_CODES.O) return
        val channel = NotificationChannel(
            NOTIFICATION_CHANNEL_ID,
            "SmartSheep notifications",
            NotificationManager.IMPORTANCE_HIGH,
        ).apply {
            description = "Reminders and alerts about your livestock"
        }
        getSystemService(NotificationManager::class.java)
            .createNotificationChannel(channel)
    }

    private companion object {
        const val NOTIFICATION_CHANNEL_ID = "smartsheep_notifications"
    }
}
