import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:provider/provider.dart';
import 'providers/auth_provider.dart';
import 'screens/login_screen.dart';
import 'screens/register_screen.dart';
import 'screens/home_screen.dart';
import 'screens/create_raffle_screen.dart';
import 'screens/raffle_detail_screen.dart';
import 'screens/claim_ticket_screen.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  final authProvider = AuthProvider();
  await authProvider.tryAutoLogin();
  runApp(
    ChangeNotifierProvider.value(
      value: authProvider,
      child: const RaffleItApp(),
    ),
  );
}

class RaffleItApp extends StatelessWidget {
  const RaffleItApp({super.key});

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();

    final router = GoRouter(
      initialLocation: auth.isAuthenticated ? '/home' : '/login',
      routes: [
        GoRoute(path: '/login', builder: (_, __) => const LoginScreen()),
        GoRoute(path: '/register', builder: (_, __) => const RegisterScreen()),
        GoRoute(path: '/home', builder: (_, __) => const HomeScreen()),
        GoRoute(path: '/create-raffle', builder: (_, __) => const CreateRaffleScreen()),
        GoRoute(
          path: '/raffles/:id',
          builder: (_, state) => RaffleDetailScreen(raffleId: state.pathParameters['id']!),
        ),
        GoRoute(
          path: '/claim/:ticketId',
          builder: (_, state) => ClaimTicketScreen(ticketId: state.pathParameters['ticketId']!),
        ),
      ],
      redirect: (context, state) {
        final isAuth = auth.isAuthenticated;
        final isPublic = state.uri.path.startsWith('/claim') ||
            state.uri.path == '/login' ||
            state.uri.path == '/register';
        if (!isAuth && !isPublic) return '/login';
        if (isAuth && (state.uri.path == '/login' || state.uri.path == '/register')) {
          return '/home';
        }
        return null;
      },
    );

    return MaterialApp.router(
      title: 'RaffleIt',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(
          seedColor: const Color(0xFF1565C0),
          brightness: Brightness.light,
        ),
        useMaterial3: true,
        inputDecorationTheme: InputDecorationTheme(
          border: OutlineInputBorder(borderRadius: BorderRadius.circular(12)),
          filled: true,
        ),
        elevatedButtonTheme: ElevatedButtonThemeData(
          style: ElevatedButton.styleFrom(
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
            padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
          ),
        ),
      ),
      routerConfig: router,
    );
  }
}
