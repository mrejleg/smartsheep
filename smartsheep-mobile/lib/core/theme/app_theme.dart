import 'package:flutter/material.dart';

abstract final class AppColors {
  static const deepGreen = Color(0xFF004B4D);
  static const teal = Color(0xFF009F97);
  static const brightTeal = Color(0xFF00B3A7);
  static const tealDark = Color(0xFF007C77);
  static const mint = Color(0xFFE6F7F5);
  static const surface = Color(0xFFF3F7FB);
  static const ink = Color(0xFF243047);
  static const muted = Color(0xFF667085);
  static const success = Color(0xFF16A34A);
  static const warning = Color(0xFFF59E0B);
  static const danger = Color(0xFFDC2626);

  /// Reminder popups are orange end to end so they never read as ordinary
  /// app chrome.
  static const reminder = Color(0xFFF57C00);
  static const reminderDark = Color(0xFFE65100);
}

abstract final class AppTheme {
  static ThemeData get light {
    final scheme = ColorScheme.fromSeed(
      seedColor: AppColors.teal,
      primary: AppColors.deepGreen,
      surface: Colors.white,
      error: AppColors.danger,
    );
    return ThemeData(
      useMaterial3: true,
      colorScheme: scheme,
      scaffoldBackgroundColor: AppColors.surface,
      fontFamily: 'Segoe UI',
      appBarTheme: const AppBarTheme(
        backgroundColor: AppColors.deepGreen,
        foregroundColor: Colors.white,
        elevation: 0,
      ),
      cardTheme: CardThemeData(
        color: Colors.white,
        elevation: 0,
        margin: EdgeInsets.zero,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(14),
          side: const BorderSide(color: Color(0xFFE5EBF0)),
        ),
      ),
      inputDecorationTheme: InputDecorationTheme(
        filled: true,
        fillColor: const Color(0xFFFBFDFD),
        border: OutlineInputBorder(
          borderRadius: BorderRadius.circular(11),
          borderSide: const BorderSide(color: Color(0xFFDDE7E7)),
        ),
        enabledBorder: OutlineInputBorder(
          borderRadius: BorderRadius.circular(11),
          borderSide: const BorderSide(color: Color(0xFFDDE7E7)),
        ),
      ),
    );
  }
}
