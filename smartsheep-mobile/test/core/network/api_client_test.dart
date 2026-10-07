import 'dart:io';

import 'package:flutter_test/flutter_test.dart';
import 'package:smartsheep_mobile/core/network/api_client.dart';
import 'package:smartsheep_mobile/core/network/secure_store.dart';

void main() {
  late HttpServer server;
  late String imageUrl;
  late _TestSecureStore store;
  late ApiClient client;

  setUp(() async {
    server = await HttpServer.bind(InternetAddress.loopbackIPv4, 0);
    imageUrl = 'http://127.0.0.1:${server.port}/news.jpg';
    store = _TestSecureStore();
    client = ApiClient(store);
  });

  tearDown(() => server.close(force: true));

  test(
    'public image requests do not read or send the API bearer token',
    () async {
      String? authorization;
      server.listen((request) async {
        authorization = request.headers.value(HttpHeaders.authorizationHeader);
        request.response.headers.contentType = ContentType('image', 'jpeg');
        request.response.add([1, 2, 3]);
        await request.response.close();
      });

      expect(await client.getBytes(imageUrl), [1, 2, 3]);
      expect(authorization, isNull);
      expect(store.tokenReads, 0);
    },
  );

  test('public image redirects do not receive the API bearer token', () async {
    final target = await HttpServer.bind(InternetAddress.loopbackIPv4, 0);
    addTearDown(() => target.close(force: true));
    final authorizationHeaders = <String?>[];
    server.listen((request) async {
      authorizationHeaders.add(
        request.headers.value(HttpHeaders.authorizationHeader),
      );
      request.response.statusCode = HttpStatus.found;
      request.response.headers.set(
        HttpHeaders.locationHeader,
        'http://127.0.0.1:${target.port}/redirected.jpg',
      );
      await request.response.close();
    });
    target.listen((request) async {
      authorizationHeaders.add(
        request.headers.value(HttpHeaders.authorizationHeader),
      );
      request.response.add([4, 5, 6]);
      await request.response.close();
    });

    expect(await client.getBytes(imageUrl), [4, 5, 6]);
    expect(authorizationHeaders, [null, null]);
    expect(store.tokenReads, 0);
  });

  test('normal API requests still receive the stored bearer token', () async {
    String? authorization;
    server.listen((request) async {
      authorization = request.headers.value(HttpHeaders.authorizationHeader);
      request.response.headers.contentType = ContentType.json;
      request.response.write('{"Data":{"ok":true}}');
      await request.response.close();
    });

    expect(await client.get(imageUrl), {'ok': true});
    expect(authorization, 'Bearer test-api-token');
    expect(store.tokenReads, 1);
  });

  test(
    'image URLs reject non-HTTP schemes before reading credentials',
    () async {
      for (final path in [
        'file:///private/image.jpg',
        'data:image/png;base64,AAAA',
        'javascript:alert(1)',
        'ftp://example.com/image.jpg',
      ]) {
        await expectLater(client.getBytes(path), throwsArgumentError);
      }
      expect(store.tokenReads, 0);
    },
  );

  test('empty image responses are reported as failures', () async {
    server.listen((request) async => request.response.close());

    await expectLater(client.getBytes(imageUrl), throwsStateError);
    expect(store.tokenReads, 0);
  });
}

class _TestSecureStore extends SecureStore {
  int tokenReads = 0;

  @override
  Future<String?> readToken() async {
    tokenReads++;
    return 'test-api-token';
  }
}
