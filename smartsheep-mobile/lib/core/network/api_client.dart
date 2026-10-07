import 'dart:io';
import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:dio/io.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

import '../config/app_config.dart';
import 'secure_store.dart';

final secureStoreProvider = Provider<SecureStore>((_) => const SecureStore());
final apiClientProvider = Provider<ApiClient>(
  (ref) => ApiClient(ref.read(secureStoreProvider)),
);

class ApiClient {
  ApiClient(this._store) {
    _dio = Dio(
      BaseOptions(
        baseUrl: AppConfig.apiBaseUrl,
        connectTimeout: const Duration(seconds: 12),
        receiveTimeout: const Duration(seconds: 20),
        sendTimeout: const Duration(seconds: 12),
        headers: const {'Accept': 'application/json'},
      ),
    );
    if (AppConfig.allowDevelopmentCertificate) {
      final adapter = _dio.httpClientAdapter;
      if (adapter is IOHttpClientAdapter) {
        adapter.createHttpClient = () => HttpClient()
          ..badCertificateCallback = (cert, host, port) =>
              host == '10.0.2.2' || host == 'localhost' || host == '127.0.0.1';
      }
    }
    _dio.interceptors.add(
      InterceptorsWrapper(
        onRequest: (options, handler) async {
          if (options.extra['publicMedia'] != true) {
            final token = await _store.readToken();
            if (token?.isNotEmpty == true) {
              options.headers['Authorization'] = 'Bearer $token';
            }
          }
          handler.next(options);
        },
      ),
    );
  }

  final SecureStore _store;
  late final Dio _dio;

  Future<dynamic> get(String path, {Map<String, dynamic>? query}) async {
    final response = await _dio.get<dynamic>(path, queryParameters: query);
    return _unwrap(response.data);
  }

  Future<Uint8List> getBytes(String path) async {
    final uri = Uri.parse(_dio.options.baseUrl).resolve(path.trim());
    if ((uri.scheme != 'http' && uri.scheme != 'https') || uri.host.isEmpty) {
      throw ArgumentError.value(path, 'path', 'Expected an HTTP image URL.');
    }

    final response = await _dio.get<List<int>>(
      uri.toString(),
      // News images are public, including when hosted outside the API origin.
      // Never send the user's API bearer token to an image server.
      options: Options(
        responseType: ResponseType.bytes,
        extra: const {'publicMedia': true},
      ),
    );
    final bytes = response.data;
    if (bytes == null || bytes.isEmpty) {
      throw StateError('The response did not contain image data.');
    }
    return Uint8List.fromList(bytes);
  }

  Future<dynamic> post(String path, {Object? data}) async {
    final response = await _dio.post<dynamic>(path, data: data);
    return _unwrap(response.data);
  }

  Future<dynamic> delete(String path, {Object? data}) async {
    final response = await _dio.delete<dynamic>(path, data: data);
    return _unwrap(response.data);
  }

  dynamic _unwrap(dynamic body) {
    if (body is Map) {
      final status = body['StatusCode'] ?? body['statusCode'];
      if (status is int && status >= 400) {
        throw DioException(
          requestOptions: RequestOptions(),
          message: '${body['Message'] ?? body['message'] ?? 'Request failed'}',
        );
      }
      return body['Data'] ?? body['data'] ?? body;
    }
    return body;
  }
}
