import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';
import '../config/app_config.dart';
import '../models/raffle.dart';
import '../models/user.dart';

class ApiException implements Exception {
  final String message;
  final int? statusCode;
  ApiException(this.message, {this.statusCode});

  @override
  String toString() => message;
}

class AuthResult {
  final String token;
  final User user;
  AuthResult(this.token, this.user);
}

class ApiService {
  final String _base = AppConfig.apiBaseUrl;

  Future<String?> _getToken() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString('token');
  }

  Future<Map<String, String>> _authHeaders() async {
    final token = await _getToken();
    return {
      'Content-Type': 'application/json',
      if (token != null) 'Authorization': 'Bearer $token',
    };
  }

  Map<String, String> get _publicHeaders => {'Content-Type': 'application/json'};

  void _checkResponse(http.Response res) {
    if (res.statusCode >= 400) {
      String message = 'Request failed';
      try {
        final body = jsonDecode(res.body);
        message = body['message'] ?? message;
      } catch (_) {}
      throw ApiException(message, statusCode: res.statusCode);
    }
  }

  Future<AuthResult> register(String email, String password, String fullName) async {
    final res = await http.post(
      Uri.parse('$_base/api/auth/register'),
      headers: _publicHeaders,
      body: jsonEncode({'email': email, 'password': password, 'fullName': fullName}),
    );
    _checkResponse(res);
    final data = jsonDecode(res.body);
    return AuthResult(data['token'], User.fromJson(data['user']));
  }

  Future<AuthResult> login(String email, String password) async {
    final res = await http.post(
      Uri.parse('$_base/api/auth/login'),
      headers: _publicHeaders,
      body: jsonEncode({'email': email, 'password': password}),
    );
    _checkResponse(res);
    final data = jsonDecode(res.body);
    return AuthResult(data['token'], User.fromJson(data['user']));
  }

  Future<List<Raffle>> getRaffles() async {
    final res = await http.get(
      Uri.parse('$_base/api/raffles'),
      headers: await _authHeaders(),
    );
    _checkResponse(res);
    final List data = jsonDecode(res.body);
    return data.map((e) => Raffle.fromJson(e)).toList();
  }

  Future<Raffle> createRaffle({
    required String name,
    String? description,
    String? organisation,
    String? prizes,
    required int numberOfTickets,
    required double ticketPrice,
    required String currency,
  }) async {
    final res = await http.post(
      Uri.parse('$_base/api/raffles'),
      headers: await _authHeaders(),
      body: jsonEncode({
        'name': name,
        'description': description,
        'organisation': organisation,
        'prizes': prizes,
        'numberOfTickets': numberOfTickets,
        'ticketPrice': ticketPrice,
        'currency': currency,
      }),
    );
    _checkResponse(res);
    return Raffle.fromJson(jsonDecode(res.body));
  }

  Future<Raffle> getRaffle(String id) async {
    final res = await http.get(
      Uri.parse('$_base/api/raffles/$id'),
      headers: await _authHeaders(),
    );
    _checkResponse(res);
    return Raffle.fromJson(jsonDecode(res.body));
  }

  String getPdfUrl(String raffleId) => '$_base/api/raffles/$raffleId/pdf';

  Future<Ticket> getTicket(String id) async {
    final res = await http.get(
      Uri.parse('$_base/api/tickets/$id'),
      headers: _publicHeaders,
    );
    _checkResponse(res);
    return Ticket.fromJson(jsonDecode(res.body));
  }

  Future<Ticket> claimTicket(String id, {
    required String name,
    required String surname,
    required String mobile,
    required String email,
    required String purchasedFrom,
  }) async {
    final res = await http.post(
      Uri.parse('$_base/api/tickets/$id/claim'),
      headers: _publicHeaders,
      body: jsonEncode({
        'name': name,
        'surname': surname,
        'mobile': mobile,
        'email': email,
        'purchasedFrom': purchasedFrom,
      }),
    );
    _checkResponse(res);
    return Ticket.fromJson(jsonDecode(res.body));
  }
}
