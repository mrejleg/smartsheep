// File generated from the SmartSheep Firebase project configuration.
// ignore_for_file: type=lint

import 'package:firebase_core/firebase_core.dart' show FirebaseOptions;
import 'package:flutter/foundation.dart'
    show TargetPlatform, defaultTargetPlatform, kIsWeb;

class DefaultFirebaseOptions {
  static FirebaseOptions get currentPlatform {
    if (kIsWeb) {
      throw UnsupportedError(
        'Firebase options are configured only for SmartSheep mobile.',
      );
    }

    return switch (defaultTargetPlatform) {
      TargetPlatform.android => android,
      TargetPlatform.iOS => ios,
      _ => throw UnsupportedError(
        'Firebase options are unavailable for $defaultTargetPlatform.',
      ),
    };
  }

  static const FirebaseOptions android = FirebaseOptions(
    apiKey: 'AIzaSyCuw98-7_lDmA_SX88Jm-oRwK-XTuqthU8',
    appId: '1:977390546151:android:510be98ea1693cf462c642',
    messagingSenderId: '977390546151',
    projectId: 'smartsheep-45a28',
    storageBucket: 'smartsheep-45a28.firebasestorage.app',
  );

  static const FirebaseOptions ios = FirebaseOptions(
    apiKey: 'AIzaSyDqzE49VY_mrr9Xx_PvpkSm6YC7h29_D6k',
    appId: '1:977390546151:ios:0dceb56bd8bf5ca062c642',
    messagingSenderId: '977390546151',
    projectId: 'smartsheep-45a28',
    storageBucket: 'smartsheep-45a28.firebasestorage.app',
    iosBundleId: 'com.pertamina.smartsheepMobile',
  );
}
