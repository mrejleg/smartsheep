import 'dart:async';
import 'dart:typed_data';

import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:smartsheep_mobile/core/network/api_client.dart';
import 'package:smartsheep_mobile/core/network/secure_store.dart';
import 'package:smartsheep_mobile/features/auth/auth_controller.dart';
import 'package:smartsheep_mobile/features/notifications/notifications_screen.dart';
import 'package:smartsheep_mobile/features/shell/header_identity.dart';

void main() {
  group('headerProfileImageProvider', () {
    test(
      'loads the actual signed-in profile image through the API endpoint',
      () async {
        final paths = <String>[];
        final bytes = Uint8List.fromList([1, 2, 3]);
        final container = _container(
          session: const AuthSession(
            token: 'test-token',
            username: 'USER.NAME',
          ),
          client: _ApiClient((path) {
            paths.add(path);
            return bytes;
          }),
        );
        addTearDown(container.dispose);
        await container.read(authControllerProvider.future);

        final image = await container.read(headerProfileImageProvider.future);

        expect(image?.bytes, same(bytes));
        expect(paths, ['/api/MobileApp/ProfileImage?username=USER.NAME']);
      },
    );

    test('encodes the session username as one query parameter', () async {
      String? requestedPath;
      final container = _container(
        session: const AuthSession(
          token: 'test-token',
          username: ' member+one@example.com&role=admin ',
        ),
        client: _ApiClient((path) {
          requestedPath = path;
          return Uint8List.fromList([1]);
        }),
      );
      addTearDown(container.dispose);
      await container.read(authControllerProvider.future);

      await container.read(headerProfileImageProvider.future);

      final uri = Uri.parse(requestedPath!);
      expect(uri.path, '/api/MobileApp/ProfileImage');
      expect(uri.queryParameters, {
        'username': 'member+one@example.com&role=admin',
      });
    });

    test(
      'signed-out and blank-username sessions never fetch an avatar',
      () async {
        for (final session in <AuthSession?>[
          null,
          const AuthSession(token: 'test-token', username: ' '),
        ]) {
          var requests = 0;
          final container = _container(
            session: session,
            client: _ApiClient((_) {
              requests++;
              return Uint8List.fromList([1]);
            }),
          );
          addTearDown(container.dispose);
          await container.read(authControllerProvider.future);

          expect(
            await container.read(headerProfileImageProvider.future),
            isNull,
          );
          expect(requests, 0);
        }
      },
    );

    test('missing images and API errors use the header fallback', () async {
      for (final client in [
        _ApiClient((_) => Uint8List(0)),
        _ApiClient((_) => throw StateError('Profile image not found')),
      ]) {
        final container = _container(
          session: const AuthSession(
            token: 'test-token',
            username: 'USER.NAME',
          ),
          client: client,
        );
        addTearDown(container.dispose);
        await container.read(authControllerProvider.future);

        expect(await container.read(headerProfileImageProvider.future), isNull);
        expect(container.read(headerProfileImageProvider).hasError, isFalse);
      }
    });

    test(
      'updates the photo when the current account changes and clears on logout',
      () async {
        final paths = <String>[];
        final auth = _AuthController(
          const AuthSession(token: 'first-token', username: 'FIRST'),
        );
        final container = ProviderContainer(
          overrides: [
            authControllerProvider.overrideWith(() => auth),
            apiClientProvider.overrideWithValue(
              _ApiClient((path) {
                paths.add(path);
                return Uint8List.fromList([paths.length]);
              }),
            ),
          ],
        );
        addTearDown(container.dispose);
        await container.read(authControllerProvider.future);
        final subscription = container.listen(
          headerProfileImageProvider,
          (_, _) {},
        );
        addTearDown(subscription.close);

        expect(
          (await container.read(headerProfileImageProvider.future))?.bytes,
          [1],
        );
        auth.setSession(
          const AuthSession(token: 'second-token', username: 'SECOND'),
        );
        expect(
          (await container.read(headerProfileImageProvider.future))?.bytes,
          [2],
        );
        auth.setSession(null);
        expect(await container.read(headerProfileImageProvider.future), isNull);
        expect(paths, [
          '/api/MobileApp/ProfileImage?username=FIRST',
          '/api/MobileApp/ProfileImage?username=SECOND',
        ]);
      },
    );
  });

  group('unreadNotificationCountProvider', () {
    test(
      'counts only real notification records explicitly marked unread',
      () async {
        final container = ProviderContainer(
          overrides: [
            notificationsProvider.overrideWith(
              (_) async => [
                {'Id': 'one', 'IsRead': false},
                {'id': 'two', 'isRead': false},
                {'Id': 'three', 'IsRead': true},
                {'id': 'four', 'isRead': true},
                {'Title': 'Welcome to SmartSheep'},
                {'Id': 'unknown', 'IsRead': null},
                {'Id': 'malformed', 'IsRead': 'false'},
                'not a record',
                null,
              ],
            ),
          ],
        );
        addTearDown(container.dispose);
        await container.read(notificationsProvider.future);

        expect(container.read(unreadNotificationCountProvider), 2);
      },
    );

    test(
      'empty, loading, and failed notification responses have no badge',
      () async {
        final pending = Completer<List<dynamic>>();
        final container = ProviderContainer(
          overrides: [
            notificationsProvider.overrideWith((_) => pending.future),
          ],
        );
        addTearDown(container.dispose);
        expect(container.read(unreadNotificationCountProvider), 0);
        pending.complete([]);
        await container.read(notificationsProvider.future);
        expect(container.read(unreadNotificationCountProvider), 0);

        final failed = ProviderContainer(
          retry: (_, _) => null,
          overrides: [
            notificationsProvider.overrideWith(
              (_) async => throw StateError('Notifications unavailable'),
            ),
          ],
        );
        addTearDown(failed.dispose);
        await expectLater(
          failed.read(notificationsProvider.future),
          throwsStateError,
        );
        expect(failed.read(unreadNotificationCountProvider), 0);
      },
    );

    test(
      'updates the badge after a notification is read and refreshed',
      () async {
        var items = <dynamic>[
          {'Id': 'one', 'IsRead': false},
          {'Id': 'two', 'IsRead': false},
        ];
        final container = ProviderContainer(
          overrides: [notificationsProvider.overrideWith((_) async => items)],
        );
        addTearDown(container.dispose);
        final subscription = container.listen(
          unreadNotificationCountProvider,
          (_, _) {},
        );
        addTearDown(subscription.close);
        await container.read(notificationsProvider.future);
        expect(container.read(unreadNotificationCountProvider), 2);

        items = [
          {'Id': 'one', 'IsRead': true},
          {'Id': 'two', 'IsRead': false},
        ];
        await container.refresh(notificationsProvider.future);

        expect(container.read(unreadNotificationCountProvider), 1);
      },
    );
  });
}

ProviderContainer _container({
  required AuthSession? session,
  required ApiClient client,
}) => ProviderContainer(
  overrides: [
    authControllerProvider.overrideWith(() => _AuthController(session)),
    apiClientProvider.overrideWithValue(client),
  ],
);

class _AuthController extends AuthController {
  _AuthController(this.session);

  final AuthSession? session;

  @override
  Future<AuthSession?> build() async => session;

  void setSession(AuthSession? session) => state = AsyncData(session);
}

class _ApiClient extends ApiClient {
  _ApiClient(this.respond) : super(const SecureStore());

  final FutureOr<Uint8List> Function(String) respond;

  @override
  Future<Uint8List> getBytes(String path) async => respond(path);
}
