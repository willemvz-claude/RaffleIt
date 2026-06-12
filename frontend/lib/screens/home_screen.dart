import 'package:flutter/material.dart';
import 'package:go_router/go_router.dart';
import 'package:intl/intl.dart';
import 'package:provider/provider.dart';
import '../models/raffle.dart';
import '../providers/auth_provider.dart';
import '../services/api_service.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  final _api = ApiService();
  List<Raffle> _raffles = [];
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() { _loading = true; _error = null; });
    try {
      final raffles = await _api.getRaffles();
      setState(() { _raffles = raffles; _loading = false; });
    } catch (e) {
      setState(() { _error = e.toString(); _loading = false; });
    }
  }

  @override
  Widget build(BuildContext context) {
    final user = context.watch<AuthProvider>().user;
    final cs = Theme.of(context).colorScheme;

    return Scaffold(
      appBar: AppBar(
        title: Row(children: [
          Icon(Icons.confirmation_number, color: cs.primary),
          const SizedBox(width: 8),
          const Text('RaffleIt', style: TextStyle(fontWeight: FontWeight.bold)),
        ]),
        actions: [
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 8),
            child: Text(user?.fullName ?? '', style: TextStyle(color: cs.onSurface)),
          ),
          IconButton(
            icon: const Icon(Icons.logout),
            tooltip: 'Sign out',
            onPressed: () {
              context.read<AuthProvider>().logout();
              context.go('/login');
            },
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: _load,
        child: _loading
            ? const Center(child: CircularProgressIndicator())
            : _error != null
                ? Center(child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(Icons.error_outline, size: 48, color: cs.error),
                      const SizedBox(height: 8),
                      Text(_error!),
                      const SizedBox(height: 16),
                      FilledButton(onPressed: _load, child: const Text('Retry')),
                    ],
                  ))
                : _raffles.isEmpty
                    ? Center(
                        child: Column(
                          mainAxisAlignment: MainAxisAlignment.center,
                          children: [
                            Icon(Icons.inbox, size: 64, color: cs.outlineVariant),
                            const SizedBox(height: 16),
                            Text('No raffles yet',
                                style: Theme.of(context).textTheme.titleLarge),
                            const SizedBox(height: 8),
                            Text('Create your first raffle to get started',
                                style: TextStyle(color: cs.onSurfaceVariant)),
                          ],
                        ),
                      )
                    : ListView.builder(
                        padding: const EdgeInsets.all(16),
                        itemCount: _raffles.length,
                        itemBuilder: (_, i) => _RaffleCard(
                          raffle: _raffles[i],
                          onTap: () => context.push('/raffles/${_raffles[i].id}').then((_) => _load()),
                        ),
                      ),
      ),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () => context.push('/create-raffle').then((_) => _load()),
        icon: const Icon(Icons.add),
        label: const Text('New Raffle'),
      ),
    );
  }
}

class _RaffleCard extends StatelessWidget {
  final Raffle raffle;
  final VoidCallback onTap;

  const _RaffleCard({required this.raffle, required this.onTap});

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final pct = raffle.numberOfTickets > 0
        ? raffle.claimedTickets / raffle.numberOfTickets
        : 0.0;

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(16),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(raffle.name,
                        style: Theme.of(context).textTheme.titleMedium?.copyWith(
                            fontWeight: FontWeight.bold)),
                  ),
                  Container(
                    padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                    decoration: BoxDecoration(
                      color: cs.primaryContainer,
                      borderRadius: BorderRadius.circular(20),
                    ),
                    child: Text(
                        '${raffle.currency} ${raffle.ticketPrice.toStringAsFixed(2)}',
                        style: TextStyle(
                            color: cs.onPrimaryContainer, fontWeight: FontWeight.bold, fontSize: 12)),
                  ),
                ],
              ),
              if (raffle.organisation != null) ...[
                const SizedBox(height: 4),
                Text(raffle.organisation!, style: TextStyle(color: cs.onSurfaceVariant, fontSize: 13)),
              ],
              const SizedBox(height: 12),
              Row(children: [
                Icon(Icons.confirmation_number_outlined, size: 16, color: cs.primary),
                const SizedBox(width: 4),
                Text('${raffle.numberOfTickets} tickets',
                    style: const TextStyle(fontSize: 13)),
                const SizedBox(width: 16),
                Icon(Icons.check_circle_outline, size: 16, color: Colors.green.shade600),
                const SizedBox(width: 4),
                Text('${raffle.claimedTickets} claimed',
                    style: const TextStyle(fontSize: 13)),
                const Spacer(),
                Text(DateFormat('d MMM yyyy').format(raffle.createdAt),
                    style: TextStyle(fontSize: 12, color: cs.onSurfaceVariant)),
              ]),
              const SizedBox(height: 8),
              ClipRRect(
                borderRadius: BorderRadius.circular(4),
                child: LinearProgressIndicator(
                  value: pct,
                  minHeight: 6,
                  backgroundColor: cs.surfaceContainerHighest,
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
