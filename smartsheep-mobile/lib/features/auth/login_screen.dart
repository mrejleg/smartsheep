import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter/services.dart';

import '../../core/theme/app_theme.dart';
import 'auth_controller.dart';

class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});
  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _username = TextEditingController();
  final _password = TextEditingController();
  final _usernameFocus = FocusNode();
  final _passwordFocus = FocusNode();
  final _form = GlobalKey<FormState>();
  bool _obscure = true;
  bool _submitting = false;

  @override
  void dispose() {
    _username.dispose();
    _password.dispose();
    _usernameFocus.dispose();
    _passwordFocus.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final keyboardInset = MediaQuery.viewInsetsOf(context).bottom;
    final keyboardVisible = keyboardInset > 0;
    ref.listen(authControllerProvider, (_, next) {
      if (next.hasError) {
        final messenger = ScaffoldMessenger.of(context);
        messenger
          ..hideCurrentSnackBar()
          ..showSnackBar(
            SnackBar(
              content: Text(friendlyLoginError(next.error)),
              behavior: SnackBarBehavior.floating,
            ),
          );
      }
    });
    return AnnotatedRegion<SystemUiOverlayStyle>(
      value: const SystemUiOverlayStyle(
        statusBarColor: Colors.transparent,
        statusBarIconBrightness: Brightness.light,
        statusBarBrightness: Brightness.dark,
        systemNavigationBarColor: Colors.white,
        systemNavigationBarIconBrightness: Brightness.dark,
      ),
      child: Scaffold(
        resizeToAvoidBottomInset: true,
        backgroundColor: const Color(0xFFF4F6F7),
        body: SafeArea(
          top: false,
          child: AutofillGroup(
            child: Form(
              key: _form,
              child: LayoutBuilder(
                builder: (context, constraints) {
                  final heroHeight = keyboardVisible
                      ? 190.0
                      : (constraints.maxHeight * .43)
                            .clamp(330.0, 390.0)
                            .toDouble();
                  final availableFormHeight =
                      constraints.maxHeight - heroHeight + 16;
                  final formHeight = availableFormHeight < 390
                      ? 390.0
                      : availableFormHeight;

                  return SingleChildScrollView(
                    physics: const AlwaysScrollableScrollPhysics(),
                    keyboardDismissBehavior:
                        ScrollViewKeyboardDismissBehavior.manual,
                    padding: EdgeInsets.only(bottom: keyboardVisible ? 12 : 0),
                    child: ConstrainedBox(
                      constraints: BoxConstraints(
                        minHeight: constraints.maxHeight,
                      ),
                      child: Column(
                        children: [
                          _LoginHero(height: heroHeight),
                          Transform.translate(
                            offset: const Offset(0, -16),
                            child: Container(
                              width: double.infinity,
                              constraints: BoxConstraints(
                                minHeight: formHeight,
                              ),
                              margin: const EdgeInsets.symmetric(horizontal: 3),
                              padding: const EdgeInsets.fromLTRB(
                                20,
                                24,
                                20,
                                28,
                              ),
                              decoration: BoxDecoration(
                                color: Colors.white,
                                borderRadius: BorderRadius.circular(18),
                                border: Border.all(
                                  color: const Color(0xFFD8DEDF),
                                ),
                                boxShadow: const [
                                  BoxShadow(
                                    color: Color(0x16001E24),
                                    blurRadius: 8,
                                    offset: Offset(0, 2),
                                  ),
                                ],
                              ),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.stretch,
                                children: [
                                  const _FieldLabel('Username'),
                                  const SizedBox(height: 8),
                                  TextFormField(
                                    key: const ValueKey('login-username'),
                                    controller: _username,
                                    focusNode: _usernameFocus,
                                    textInputAction: TextInputAction.next,
                                    keyboardType: TextInputType.text,
                                    textCapitalization: TextCapitalization.none,
                                    autocorrect: false,
                                    enableSuggestions: false,
                                    autofillHints: const [
                                      AutofillHints.username,
                                    ],
                                    scrollPadding: const EdgeInsets.only(
                                      bottom: 160,
                                    ),
                                    style: const TextStyle(fontSize: 14),
                                    decoration: _fieldDecoration(
                                      hint: 'Enter username',
                                      icon: Icons.person_outline_rounded,
                                    ),
                                    validator: (value) =>
                                        value == null || value.trim().isEmpty
                                        ? 'Username is required'
                                        : null,
                                    onFieldSubmitted: (_) => FocusScope.of(
                                      context,
                                    ).requestFocus(_passwordFocus),
                                  ),
                                  const SizedBox(height: 18),
                                  const _FieldLabel('Password'),
                                  const SizedBox(height: 8),
                                  TextFormField(
                                    key: const ValueKey('login-password'),
                                    controller: _password,
                                    focusNode: _passwordFocus,
                                    obscureText: _obscure,
                                    textInputAction: TextInputAction.done,
                                    keyboardType: TextInputType.visiblePassword,
                                    autocorrect: false,
                                    enableSuggestions: false,
                                    autofillHints: const [
                                      AutofillHints.password,
                                    ],
                                    scrollPadding: const EdgeInsets.only(
                                      bottom: 160,
                                    ),
                                    style: const TextStyle(fontSize: 14),
                                    onFieldSubmitted: (_) => _submit(),
                                    decoration: _fieldDecoration(
                                      hint: 'Enter password',
                                      icon: Icons.lock_outline_rounded,
                                      suffix: IconButton(
                                        visualDensity: VisualDensity.compact,
                                        onPressed: () => setState(
                                          () => _obscure = !_obscure,
                                        ),
                                        icon: Icon(
                                          _obscure
                                              ? Icons.visibility_outlined
                                              : Icons.visibility_off_outlined,
                                          size: 19,
                                          color: const Color(0xFF676DBB),
                                        ),
                                      ),
                                    ),
                                    validator: (value) =>
                                        value == null || value.isEmpty
                                        ? 'Password is required'
                                        : null,
                                  ),
                                  const SizedBox(height: 28),
                                  DecoratedBox(
                                    decoration: BoxDecoration(
                                      gradient: const LinearGradient(
                                        colors: [
                                          Color(0xFF00464B),
                                          Color(0xFF006467),
                                        ],
                                      ),
                                      borderRadius: BorderRadius.circular(6),
                                      boxShadow: const [
                                        BoxShadow(
                                          color: Color(0x26004B4D),
                                          blurRadius: 8,
                                          offset: Offset(0, 3),
                                        ),
                                      ],
                                    ),
                                    child: FilledButton(
                                      onPressed: _submitting ? null : _submit,
                                      style: FilledButton.styleFrom(
                                        minimumSize: const Size.fromHeight(50),
                                        backgroundColor: Colors.transparent,
                                        disabledBackgroundColor: Colors.white
                                            .withValues(alpha: .18),
                                        shadowColor: Colors.transparent,
                                        shape: RoundedRectangleBorder(
                                          borderRadius: BorderRadius.circular(
                                            6,
                                          ),
                                        ),
                                      ),
                                      child: _submitting
                                          ? const SizedBox.square(
                                              dimension: 18,
                                              child: CircularProgressIndicator(
                                                strokeWidth: 2,
                                                color: Colors.white,
                                              ),
                                            )
                                          : const Row(
                                              mainAxisSize: MainAxisSize.min,
                                              mainAxisAlignment:
                                                  MainAxisAlignment.center,
                                              children: [
                                                Icon(
                                                  Icons.login_rounded,
                                                  size: 20,
                                                ),
                                                SizedBox(width: 9),
                                                Text(
                                                  'Sign In',
                                                  style: TextStyle(
                                                    fontSize: 15,
                                                    fontWeight: FontWeight.w700,
                                                  ),
                                                ),
                                              ],
                                            ),
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                  );
                },
              ),
            ),
          ),
        ),
      ),
    );
  }

  Future<void> _submit() async {
    if (_form.currentState?.validate() != true) return;
    TextInput.finishAutofillContext();
    FocusManager.instance.primaryFocus?.unfocus();
    setState(() => _submitting = true);
    await ref
        .read(authControllerProvider.notifier)
        .login(_username.text, _password.text);
    if (mounted) setState(() => _submitting = false);
  }
}

String friendlyLoginError(Object? error) {
  if (error is StateError) {
    return 'The application login configuration is incomplete.';
  }

  if (error is! DioException) {
    return 'Unable to sign in. Please try again.';
  }

  final statusCode = error.response?.statusCode;
  final responseData = error.response?.data;
  if (statusCode == 400) {
    if (_hasValidationField(responseData, 'ClientId') ||
        _hasValidationField(responseData, 'ClientSecret')) {
      return 'The application login configuration is incomplete.';
    }
    return 'The login request is invalid. Please check your input.';
  }
  if (statusCode == 401) {
    final message = _responseMessage(responseData);
    if (message?.toLowerCase().contains('client') == true) {
      return 'The application login configuration is invalid.';
    }
    return message ?? 'The username or password is incorrect.';
  }
  if (statusCode != null && statusCode >= 500) {
    return 'The SmartSheep server is temporarily unavailable.';
  }

  switch (error.type) {
    case DioExceptionType.connectionTimeout:
    case DioExceptionType.sendTimeout:
    case DioExceptionType.receiveTimeout:
      return 'The server took too long to respond. Please try again.';
    case DioExceptionType.connectionError:
    case DioExceptionType.badCertificate:
      return 'Unable to connect to the SmartSheep server.';
    default:
      return _responseMessage(responseData) ??
          'Unable to sign in. Please try again.';
  }
}

bool _hasValidationField(dynamic data, String field) {
  if (data is! Map) return false;
  final errors = data['errors'] ?? data['Errors'];
  if (errors is! Map) return false;
  return errors.keys.any(
    (key) => key.toString().toLowerCase() == field.toLowerCase(),
  );
}

String? _responseMessage(dynamic data) {
  if (data is! Map) return null;
  final value = data['Message'] ?? data['message'];
  if (value is! String || value.trim().isEmpty) return null;
  return value.trim();
}

InputDecoration _fieldDecoration({
  required String hint,
  required IconData icon,
  Widget? suffix,
}) => InputDecoration(
  hintText: hint,
  hintStyle: const TextStyle(fontSize: 13, color: Color(0xFF777E91)),
  prefixIcon: Icon(icon, size: 19, color: const Color(0xFF777BC4)),
  prefixIconConstraints: const BoxConstraints(minWidth: 43),
  suffixIcon: suffix,
  suffixIconConstraints: const BoxConstraints(minWidth: 43),
  filled: true,
  fillColor: Colors.white,
  contentPadding: const EdgeInsets.symmetric(horizontal: 12, vertical: 14),
  isDense: true,
  border: OutlineInputBorder(
    borderRadius: BorderRadius.circular(7),
    borderSide: const BorderSide(color: Color(0xFFDDE4E8)),
  ),
  enabledBorder: OutlineInputBorder(
    borderRadius: BorderRadius.circular(7),
    borderSide: const BorderSide(color: Color(0xFFDDE4E8)),
  ),
  focusedBorder: OutlineInputBorder(
    borderRadius: BorderRadius.circular(7),
    borderSide: const BorderSide(color: AppColors.tealDark, width: 1.4),
  ),
);

class _FieldLabel extends StatelessWidget {
  const _FieldLabel(this.text);

  final String text;

  @override
  Widget build(BuildContext context) => Text(
    text,
    style: const TextStyle(
      color: Color(0xFF28315A),
      fontSize: 12,
      fontWeight: FontWeight.w700,
    ),
  );
}

class _LoginHero extends StatelessWidget {
  const _LoginHero({required this.height});

  final double height;

  @override
  Widget build(BuildContext context) {
    final compact = height < 250;
    return Container(
      height: height,
      width: double.infinity,
      decoration: const BoxDecoration(
        gradient: LinearGradient(
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
          colors: [Color(0xFF003E44), Color(0xFF005C61), Color(0xFF007779)],
        ),
      ),
      child: Stack(
        children: [
          const Positioned(
            left: 0,
            right: 0,
            bottom: 0,
            height: 120,
            child: Image(
              image: AssetImage('assets/images/login-pasture-silhouette.png'),
              fit: BoxFit.cover,
              alignment: Alignment.bottomCenter,
              color: Color(0xFF002F35),
              colorBlendMode: BlendMode.srcIn,
              filterQuality: FilterQuality.medium,
            ),
          ),
          Padding(
            padding: EdgeInsets.fromLTRB(28, compact ? 28 : 55, 28, 0),
            child: Column(
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    Image.asset(
                      'assets/images/smartsheep-mark.png',
                      width: 55,
                      height: 55,
                    ),
                    const SizedBox(width: 11),
                    const Flexible(
                      child: FittedBox(
                        fit: BoxFit.scaleDown,
                        child: Text.rich(
                          TextSpan(
                            children: [
                              TextSpan(
                                text: 'Smart',
                                style: TextStyle(color: Colors.white),
                              ),
                              TextSpan(
                                text: 'Sheep',
                                style: TextStyle(color: AppColors.brightTeal),
                              ),
                            ],
                          ),
                          style: TextStyle(
                            fontSize: 28,
                            fontWeight: FontWeight.w700,
                            letterSpacing: -.5,
                          ),
                        ),
                      ),
                    ),
                  ],
                ),
                SizedBox(height: compact ? 12 : 25),
                const Text(
                  'Welcome back',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 18,
                    fontWeight: FontWeight.w700,
                  ),
                ),
                const SizedBox(height: 8),
                const Text(
                  'Sign in to monitor your livestock,\nanytime, anywhere.',
                  textAlign: TextAlign.center,
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 13,
                    height: 1.35,
                  ),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
