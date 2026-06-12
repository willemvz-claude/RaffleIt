import 'package:flutter/material.dart';
import '../models/raffle.dart';
import '../services/api_service.dart';

class ClaimTicketScreen extends StatefulWidget {
  final String ticketId;
  const ClaimTicketScreen({super.key, required this.ticketId});

  @override
  State<ClaimTicketScreen> createState() => _ClaimTicketScreenState();
}

class _ClaimTicketScreenState extends State<ClaimTicketScreen> {
  final _api = ApiService();
  Ticket? _ticket;
  bool _loading = true;
  bool _submitting = false;
  String? _error;
  bool _claimed = false;

  final _form = GlobalKey<FormState>();
  final _nameCtrl = TextEditingController();
  final _surnameCtrl = TextEditingController();
  final _mobileCtrl = TextEditingController();
  final _emailCtrl = TextEditingController();
  final _purchasedCtrl = TextEditingController();

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    _nameCtrl.dispose();
    _surnameCtrl.dispose();
    _mobileCtrl.dispose();
    _emailCtrl.dispose();
    _purchasedCtrl.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() { _loading = true; _error = null; });
    try {
      final ticket = await _api.getTicket(widget.ticketId);
      setState(() { _ticket = ticket; _loading = false; });
      if (ticket.isClaimed) {
        _nameCtrl.text = ticket.claimedName ?? '';
        _surnameCtrl.text = ticket.claimedSurname ?? '';
        _mobileCtrl.text = ticket.claimedMobile ?? '';
        _emailCtrl.text = ticket.claimedEmail ?? '';
        _purchasedCtrl.text = ticket.purchasedFrom ?? '';
      }
    } catch (e) {
      setState(() { _error = e.toString(); _loading = false; });
    }
  }

  Future<void> _submit() async {
    if (!_form.currentState!.validate()) return;
    setState(() { _submitting = true; _error = null; });
    try {
      final updated = await _api.claimTicket(
        widget.ticketId,
        name: _nameCtrl.text.trim(),
        surname: _surnameCtrl.text.trim(),
        mobile: _mobileCtrl.text.trim(),
        email: _emailCtrl.text.trim(),
        purchasedFrom: _purchasedCtrl.text.trim(),
      );
      setState(() { _ticket = updated; _claimed = true; _submitting = false; });
    } catch (e) {
      setState(() { _error = e.toString(); _submitting = false; });
    }
  }

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;

    return Scaffold(
      backgroundColor: cs.surfaceContainerLowest,
      body: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(16),
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 480),
            child: Column(
              children: [
                const SizedBox(height: 32),
                Icon(Icons.confirmation_number, size: 48, color: cs.primary),
                const SizedBox(height: 8),
                Text('RaffleIt', style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                    fontWeight: FontWeight.bold, color: cs.primary)),
                const SizedBox(height: 24),
                if (_loading)
                  const CircularProgressIndicator()
                else if (_error != null && _ticket == null)
                  Card(
                    shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                    child: Padding(
                      padding: const EdgeInsets.all(24),
                      child: Column(children: [
                        Icon(Icons.error_outline, size: 48, color: cs.error),
                        const SizedBox(height: 8),
                        Text('Ticket not found', style: Theme.of(context).textTheme.titleLarge),
                        const SizedBox(height: 4),
                        Text('This ticket may not exist or the link is invalid.',
                            textAlign: TextAlign.center,
                            style: TextStyle(color: cs.onSurfaceVariant)),
                      ]),
                    ),
                  )
                else if (_ticket != null)
                  Column(children: [
                    _TicketInfoCard(ticket: _ticket!),
                    const SizedBox(height: 16),
                    if (_claimed || _ticket!.isClaimed)
                      _SuccessCard(ticket: _ticket!)
                    else
                      _ClaimForm(
                        formKey: _form,
                        nameCtrl: _nameCtrl,
                        surnameCtrl: _surnameCtrl,
                        mobileCtrl: _mobileCtrl,
                        emailCtrl: _emailCtrl,
                        purchasedCtrl: _purchasedCtrl,
                        error: _error,
                        submitting: _submitting,
                        onSubmit: _submit,
                      ),
                  ]),
                const SizedBox(height: 32),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _TicketInfoCard extends StatelessWidget {
  final Ticket ticket;
  const _TicketInfoCard({required this.ticket});

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    final raffle = ticket.raffle;

    return Card(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      color: cs.primaryContainer,
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(children: [
              Expanded(
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text(raffle?.name ?? 'Raffle',
                      style: Theme.of(context).textTheme.titleLarge?.copyWith(
                          fontWeight: FontWeight.bold, color: cs.onPrimaryContainer)),
                  if (raffle?.organisation != null)
                    Text(raffle!.organisation!,
                        style: TextStyle(color: cs.onPrimaryContainer.withOpacity(0.8))),
                ]),
              ),
              Container(
                padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
                decoration: BoxDecoration(
                  color: cs.primary,
                  borderRadius: BorderRadius.circular(12),
                ),
                child: Text(
                  '#${ticket.ticketNumber.toString().padLeft(4, '0')}',
                  style: TextStyle(
                      color: cs.onPrimary, fontWeight: FontWeight.bold, fontSize: 20),
                ),
              ),
            ]),
            if (raffle?.prizes != null) ...[
              const SizedBox(height: 12),
              Row(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Icon(Icons.emoji_events, color: Colors.amber.shade700, size: 18),
                const SizedBox(width: 6),
                Expanded(child: Text(raffle!.prizes!,
                    style: TextStyle(color: cs.onPrimaryContainer.withOpacity(0.9), fontSize: 13))),
              ]),
            ],
            if (raffle != null) ...[
              const SizedBox(height: 8),
              Text('${raffle.currency} ${raffle.ticketPrice.toStringAsFixed(2)} per ticket',
                  style: TextStyle(
                      color: cs.onPrimaryContainer.withOpacity(0.7), fontSize: 13)),
            ],
          ],
        ),
      ),
    );
  }
}

class _SuccessCard extends StatelessWidget {
  final Ticket ticket;
  const _SuccessCard({required this.ticket});

  @override
  Widget build(BuildContext context) {
    return Card(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      color: Colors.green.shade50,
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(children: [
          Icon(Icons.check_circle, size: 56, color: Colors.green.shade600),
          const SizedBox(height: 8),
          Text('Ticket Claimed!',
              style: Theme.of(context).textTheme.titleLarge?.copyWith(
                  fontWeight: FontWeight.bold, color: Colors.green.shade700)),
          const SizedBox(height: 8),
          Text('This ticket is registered to:',
              style: TextStyle(color: Colors.grey.shade700)),
          const SizedBox(height: 8),
          Text('${ticket.claimedName} ${ticket.claimedSurname}',
              style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 18)),
          Text(ticket.claimedEmail ?? '',
              style: TextStyle(color: Colors.grey.shade600)),
          if (ticket.purchasedFrom != null) ...[
            const SizedBox(height: 8),
            Text('Purchased from: ${ticket.purchasedFrom}',
                style: TextStyle(color: Colors.grey.shade600, fontSize: 13)),
          ],
        ]),
      ),
    );
  }
}

class _ClaimForm extends StatelessWidget {
  final GlobalKey<FormState> formKey;
  final TextEditingController nameCtrl, surnameCtrl, mobileCtrl, emailCtrl, purchasedCtrl;
  final String? error;
  final bool submitting;
  final VoidCallback onSubmit;

  const _ClaimForm({
    required this.formKey,
    required this.nameCtrl,
    required this.surnameCtrl,
    required this.mobileCtrl,
    required this.emailCtrl,
    required this.purchasedCtrl,
    required this.error,
    required this.submitting,
    required this.onSubmit,
  });

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;

    return Card(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Form(
          key: formKey,
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              Text('Register Your Ticket',
                  style: Theme.of(context).textTheme.titleMedium?.copyWith(
                      fontWeight: FontWeight.bold, color: cs.primary)),
              const SizedBox(height: 4),
              Text('Fill in your details to claim this ticket',
                  style: TextStyle(color: cs.onSurfaceVariant, fontSize: 13)),
              const SizedBox(height: 16),
              if (error != null)
                Container(
                  margin: const EdgeInsets.only(bottom: 12),
                  padding: const EdgeInsets.all(10),
                  decoration: BoxDecoration(
                    color: cs.errorContainer,
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Text(error!, style: TextStyle(color: cs.onErrorContainer)),
                ),
              Row(children: [
                Expanded(child: TextFormField(
                  controller: nameCtrl,
                  decoration: const InputDecoration(labelText: 'First Name *'),
                  validator: (v) => v == null || v.trim().isEmpty ? 'Required' : null,
                )),
                const SizedBox(width: 12),
                Expanded(child: TextFormField(
                  controller: surnameCtrl,
                  decoration: const InputDecoration(labelText: 'Surname *'),
                  validator: (v) => v == null || v.trim().isEmpty ? 'Required' : null,
                )),
              ]),
              const SizedBox(height: 12),
              TextFormField(
                controller: mobileCtrl,
                decoration: const InputDecoration(
                  labelText: 'Mobile Number *',
                  prefixIcon: Icon(Icons.phone_outlined),
                ),
                keyboardType: TextInputType.phone,
                validator: (v) => v == null || v.trim().isEmpty ? 'Required' : null,
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: emailCtrl,
                decoration: const InputDecoration(
                  labelText: 'Email Address *',
                  prefixIcon: Icon(Icons.email_outlined),
                ),
                keyboardType: TextInputType.emailAddress,
                validator: (v) =>
                    v == null || !v.contains('@') ? 'Enter valid email' : null,
              ),
              const SizedBox(height: 12),
              TextFormField(
                controller: purchasedCtrl,
                decoration: const InputDecoration(
                  labelText: 'Purchased From *',
                  hintText: 'Who sold you this ticket?',
                  prefixIcon: Icon(Icons.person_outlined),
                ),
                validator: (v) => v == null || v.trim().isEmpty ? 'Required' : null,
              ),
              const SizedBox(height: 20),
              submitting
                  ? const Center(child: CircularProgressIndicator())
                  : FilledButton.icon(
                      onPressed: onSubmit,
                      icon: const Icon(Icons.how_to_reg),
                      label: const Text('Claim My Ticket'),
                    ),
            ],
          ),
        ),
      ),
    );
  }
}
