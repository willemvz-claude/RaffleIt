// ignore: avoid_web_libraries_in_flutter
import 'dart:html' as html;
import 'package:flutter/material.dart';
import 'package:intl/intl.dart';
import '../models/raffle.dart';
import '../services/api_service.dart';

class RaffleDetailScreen extends StatefulWidget {
  final String raffleId;
  const RaffleDetailScreen({super.key, required this.raffleId});

  @override
  State<RaffleDetailScreen> createState() => _RaffleDetailScreenState();
}

class _RaffleDetailScreenState extends State<RaffleDetailScreen> {
  final _api = ApiService();
  Raffle? _raffle;
  bool _loading = true;
  String? _error;
  bool _pdfLoading = false;
  String _filter = 'all';

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final raffle = await _api.getRaffle(widget.raffleId);
      setState(() {
        _raffle = raffle;
        _loading = false;
      });
    } catch (e) {
      setState(() {
        _error = e.toString();
        _loading = false;
      });
    }
  }

  Future<void> _downloadPdf() async {
    setState(() => _pdfLoading = true);
    try {
      final url = _api.getPdfUrl(widget.raffleId);
      html.AnchorElement(href: url)
        ..setAttribute('download', '${_raffle!.name}_tickets.pdf')
        ..click();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error: $e'), backgroundColor: Colors.red),
        );
      }
    } finally {
      setState(() => _pdfLoading = false);
    }
  }

  List<Ticket> get _filteredTickets {
    if (_raffle?.tickets == null) return [];
    switch (_filter) {
      case 'claimed':
        return _raffle!.tickets!.where((t) => t.isClaimed).toList();
      case 'unclaimed':
        return _raffle!.tickets!.where((t) => !t.isClaimed).toList();
      default:
        return _raffle!.tickets!;
    }
  }

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;

    return Scaffold(
      appBar: AppBar(
        title: Text(_raffle?.name ?? 'Raffle Detail',
            style: const TextStyle(fontWeight: FontWeight.bold)),
        actions: [
          if (_raffle != null)
            _pdfLoading
                ? const Padding(
                    padding: EdgeInsets.all(12),
                    child: SizedBox(
                        width: 24,
                        height: 24,
                        child: CircularProgressIndicator(strokeWidth: 2)))
                : IconButton(
                    icon: const Icon(Icons.picture_as_pdf),
                    tooltip: 'Download PDF Tickets',
                    onPressed: _downloadPdf,
                  ),
        ],
      ),
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? Center(
                  child: Column(
                    mainAxisAlignment: MainAxisAlignment.center,
                    children: [
                      Icon(Icons.error_outline, size: 48, color: cs.error),
                      const SizedBox(height: 8),
                      Text(_error!),
                      const SizedBox(height: 16),
                      FilledButton(onPressed: _load, child: const Text('Retry')),
                    ],
                  ))
              : _raffle == null
                  ? const SizedBox()
                  : SingleChildScrollView(
                      padding: const EdgeInsets.all(16),
                      child: Center(
                        child: ConstrainedBox(
                          constraints: const BoxConstraints(maxWidth: 960),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              _RaffleInfoCard(raffle: _raffle!),
                              const SizedBox(height: 16),
                              _TicketStatsBar(raffle: _raffle!),
                              const SizedBox(height: 16),
                              Card(
                                shape: RoundedRectangleBorder(
                                    borderRadius: BorderRadius.circular(16)),
                                child: Padding(
                                  padding: const EdgeInsets.all(16),
                                  child: Column(
                                    crossAxisAlignment: CrossAxisAlignment.start,
                                    children: [
                                      Row(children: [
                                        Text('Tickets',
                                            style: Theme.of(context)
                                                .textTheme
                                                .titleMedium
                                                ?.copyWith(
                                                    fontWeight:
                                                        FontWeight.bold)),
                                        const Spacer(),
                                        SegmentedButton<String>(
                                          segments: const [
                                            ButtonSegment(
                                                value: 'all',
                                                label: Text('All')),
                                            ButtonSegment(
                                                value: 'claimed',
                                                label: Text('Claimed')),
                                            ButtonSegment(
                                                value: 'unclaimed',
                                                label: Text('Unclaimed')),
                                          ],
                                          selected: {_filter},
                                          onSelectionChanged: (v) =>
                                              setState(() =>
                                                  _filter = v.first),
                                        ),
                                      ]),
                                      const SizedBox(height: 12),
                                      if (_filteredTickets.isEmpty)
                                        const Center(
                                          child: Padding(
                                            padding: EdgeInsets.all(32),
                                            child: Text(
                                                'No tickets match this filter'),
                                          ),
                                        )
                                      else
                                        ListView.separated(
                                          shrinkWrap: true,
                                          physics:
                                              const NeverScrollableScrollPhysics(),
                                          itemCount: _filteredTickets.length,
                                          separatorBuilder: (_, __) =>
                                              const Divider(height: 1),
                                          itemBuilder: (_, i) => _TicketRow(
                                              ticket: _filteredTickets[i]),
                                        ),
                                    ],
                                  ),
                                ),
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),
    );
  }
}

class _RaffleInfoCard extends StatelessWidget {
  final Raffle raffle;
  const _RaffleInfoCard({required this.raffle});

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Card(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      color: cs.primaryContainer,
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(raffle.name,
                style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                    fontWeight: FontWeight.bold,
                    color: cs.onPrimaryContainer)),
            if (raffle.organisation != null) ...[
              const SizedBox(height: 4),
              Text(raffle.organisation!,
                  style: TextStyle(
                      color: cs.onPrimaryContainer.withOpacity(0.8))),
            ],
            if (raffle.description != null) ...[
              const SizedBox(height: 8),
              Text(raffle.description!,
                  style: TextStyle(
                      color: cs.onPrimaryContainer.withOpacity(0.9))),
            ],
            if (raffle.prizes != null) ...[
              const SizedBox(height: 12),
              Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Icon(Icons.emoji_events,
                    color: Colors.amber.shade700, size: 20),
                const SizedBox(width: 8),
                Expanded(
                    child: Text(raffle.prizes!,
                        style:
                            TextStyle(color: cs.onPrimaryContainer))),
              ]),
            ],
            const SizedBox(height: 12),
            Row(children: [
              Icon(Icons.calendar_today_outlined,
                  size: 16,
                  color: cs.onPrimaryContainer.withOpacity(0.7)),
              const SizedBox(width: 4),
              Text(DateFormat('d MMMM yyyy').format(raffle.createdAt),
                  style: TextStyle(
                      color: cs.onPrimaryContainer.withOpacity(0.7),
                      fontSize: 13)),
            ]),
          ],
        ),
      ),
    );
  }
}

class _TicketStatsBar extends StatelessWidget {
  final Raffle raffle;
  const _TicketStatsBar({required this.raffle});

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final pct = raffle.numberOfTickets > 0
        ? raffle.claimedTickets / raffle.numberOfTickets
        : 0.0;

    return Card(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Row(children: [
          Expanded(
              child: _Stat(
                  icon: Icons.confirmation_number_outlined,
                  label: 'Total',
                  value: raffle.numberOfTickets.toString(),
                  color: cs.primary)),
          Expanded(
              child: _Stat(
                  icon: Icons.check_circle_outline,
                  label: 'Claimed',
                  value: raffle.claimedTickets.toString(),
                  color: Colors.green.shade600)),
          Expanded(
              child: _Stat(
                  icon: Icons.radio_button_unchecked,
                  label: 'Available',
                  value: (raffle.numberOfTickets - raffle.claimedTickets)
                      .toString(),
                  color: Colors.orange.shade600)),
          Expanded(
              child: _Stat(
                  icon: Icons.attach_money,
                  label: 'Price',
                  value:
                      '${raffle.currency} ${raffle.ticketPrice.toStringAsFixed(2)}',
                  color: cs.tertiary)),
          Column(children: [
            Text('${(pct * 100).toStringAsFixed(0)}%',
                style: TextStyle(
                    fontWeight: FontWeight.bold, color: cs.primary)),
            const SizedBox(height: 4),
            SizedBox(
              width: 60,
              child: ClipRRect(
                borderRadius: BorderRadius.circular(4),
                child: LinearProgressIndicator(value: pct, minHeight: 8),
              ),
            ),
          ]),
        ]),
      ),
    );
  }
}

class _Stat extends StatelessWidget {
  final IconData icon;
  final String label;
  final String value;
  final Color color;

  const _Stat(
      {required this.icon,
      required this.label,
      required this.value,
      required this.color});

  @override
  Widget build(BuildContext context) {
    return Column(children: [
      Icon(icon, color: color, size: 24),
      const SizedBox(height: 4),
      Text(value,
          style: TextStyle(
              fontWeight: FontWeight.bold, fontSize: 16, color: color)),
      Text(label,
          style: TextStyle(
              fontSize: 12,
              color: Theme.of(context).colorScheme.onSurfaceVariant)),
    ]);
  }
}

class _TicketRow extends StatelessWidget {
  final Ticket ticket;
  const _TicketRow({required this.ticket});

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return ListTile(
      leading: CircleAvatar(
        backgroundColor:
            ticket.isClaimed ? Colors.green.shade100 : cs.primaryContainer,
        child: Text(
          ticket.ticketNumber.toString().padLeft(4, '0'),
          style: TextStyle(
            fontSize: 11,
            fontWeight: FontWeight.bold,
            color: ticket.isClaimed
                ? Colors.green.shade700
                : cs.onPrimaryContainer,
          ),
        ),
      ),
      title: ticket.isClaimed
          ? Text('${ticket.claimedName} ${ticket.claimedSurname}',
              style: const TextStyle(fontWeight: FontWeight.w500))
          : Text('Unclaimed',
              style: TextStyle(color: cs.onSurfaceVariant)),
      subtitle: ticket.isClaimed && ticket.claimedEmail != null
          ? Text(ticket.claimedEmail!, style: const TextStyle(fontSize: 12))
          : null,
      trailing: ticket.isClaimed
          ? Icon(Icons.check_circle, color: Colors.green.shade600)
          : Icon(Icons.radio_button_unchecked, color: cs.outlineVariant),
    );
  }
}
