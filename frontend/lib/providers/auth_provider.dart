import 'package:flutter/foundation.dart';
import 'package:shared_preferences/shared_preferences.dart';
import '../models/user.dart';
import '../services/api_service.dart';

class AuthProvider extends ChangeNotifier {
  final ApiService _api = ApiService();
  User? _user;
  bool _loading = false;

  User? get user => _user;
  bool get isAuthenticated => _user != null;
  bool get loading => _loading;

  Future<void> tryAutoLogin() async {
    final prefs = await SharedPreferences.getInstance();
    final token = prefs.getString('token');
    final id = prefs.getString('userId');
    final email = prefs.getString('userEmail');
    final name = prefs.getString('userName');
    if (token != null && id != null && email != null && name != null) {
      _user = User(id: id, email: email, fullName: name);
      notifyListeners();
    }
  }

  Future<void> register(String email, String password, String fullName) async {
    _loading = true;
    notifyListeners();
    try {
      final result = await _api.register(email, password, fullName);
      await _saveSession(result);
      _user = result.user;
    } finally {
      _loading = false;
      notifyListeners();
    }
  }

  Future<void> login(String email, String password) async {
    _loading = true;
    notifyListeners();
    try {
      final result = await _api.login(email, password);
      await _saveSession(result);
      _user = result.user;
    } finally {
      _loading = false;
      notifyListeners();
    }
  }

  Future<void> logout() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.clear();
    _user = null;
    notifyListeners();
  }

  Future<void> _saveSession(AuthResult result) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('token', result.token);
    await prefs.setString('userId', result.user.id);
    await prefs.setString('userEmail', result.user.email);
    await prefs.setString('userName', result.user.fullName);
  }
}
