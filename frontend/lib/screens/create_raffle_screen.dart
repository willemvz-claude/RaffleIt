import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:go_router/go_router.dart';
import '../services/api_service.dart';

class CreateRaffleScreen extends StatefulWidget {
  const CreateRaffleScreen({super.key});

  @override
  State<CreateRaffleScreen> createState() => _CreateRaffleScreenState();
}

class _CreateRaffleScreenState extends State<CreateRaffleScreen> {
  final _form = GlobalKey<FormState>();
  final _api = ApiService();

  final _nameCtrl = TextEditingController();
  final _descCtrl = TextEditingController();
  final _orgCtrl = TextEditingController();
  final _prizesCtrl = TextEditingController();
  final _ticketsCtrl = TextEditingController();
  final _priceCtrl = TextEditingController();
  String _currency = 'USD';
  bool _loading = false;
  String? _error;

  static const _currencies = ['USD', 'EUR', 'GBP', 'ZAR', 'AUD', 'CAD', 'NZD', 'CHF'];

  @override
  void dispose() {
    _nameCtrl.dispose();
    _descCtrl.dispose();
    _orgCtrl.dispose();
    _prizesCtrl.dispose();
    _ticketsCtrl.dispose();
    _priceCtrl.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_form.currentState!.validate()) return;
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      await _api.createRaffle(
        name: _nameCtrl.text.trim(),
        description: _descCtrl.text.trim().isEmpty ? null : _descCtrl.text.trim(),
        organisation: _orgCtrl.text.trim().isEmpty ? null : _orgCtrl.text.trim(),
        prizes: _prizesCtrl.text.trim().isEmpty ? null : _prizesCtrl.text.trim(),
        numberOfTickets: int.parse(_ticketsCtrl.text.trim()),
        ticketPrice: double.parse(_priceCtrl.text.trim()),
        currency: _currency,
      );
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
              content: Text('Raffle created successfully!'),
              backgroundColor: Colors.green),
        );
        context.pop();
      }
    } catch (e) {
      setState(() => _error = e.toString());
    } finally {
      setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Create Raffle',
            style: TextStyle(fontWeight: FontWeight.bold)),
        leading:
            IconButton(icon: const Icon(Icons.arrow_back), onPressed: () => context.pop()),
      ),
      body: SingleChildScrollView(
        padding: const EdgeInsets.all(16),
        child: Center(
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 640),
            child: Form(
              key: _form,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  if (_error != null)
                    Container(
                      margin: const EdgeInsets.only(bottom: 16),
                      padding: const EdgeInsets.all(12),
                      decoration: BoxDecoration(
                        color: cs.errorContainer,
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child:
                          Text(_error!, style: TextStyle(color: cs.onErrorContainer)),
                    ),
                  _Section(title: 'Basic Information', children: [
                    TextFormField(
                      controller: _nameCtrl,
                      decoration: const InputDecoration(
                        labelText: 'Raffle Name *',
                        hintText: 'e.g. Annual Charity Raffle 2024',
                        prefixIcon: Icon(Icons.confirmation_number_outlined),
                      ),
                      validator: (v) =>
                          v == null || v.trim().isEmpty ? 'Required' : null,
                    ),
                    const SizedBox(height: 16),
                    TextFormField(
                      controller: _orgCtrl,
                      decoration: const InputDecoration(
                        labelText: 'Organisation',
                        hintText: 'e.g. Lions Club',
                        prefixIcon: Icon(Icons.business_outlined),
                      ),
                    ),
                    const SizedBox(height: 16),
                    TextFormField(
                      controller: _descCtrl,
                      maxLines: 3,
                      decoration: const InputDecoration(
                        labelText: 'Description',
                        hintText: 'Describe your raffle...',
                        prefixIcon: Icon(Icons.description_outlined),
                        alignLabelWithHint: true,
                      ),
                    ),
                  ]),
                  const SizedBox(height: 16),
                  _Section(title: 'Prizes', children: [
                    TextFormField(
                      controller: _prizesCtrl,
                      maxLines: 4,
                      decoration: const InputDecoration(
                        labelText: 'Prizes',
                        hintText: '1st: Car\n2nd: Holiday\n3rd: Cash',
                        prefixIcon: Icon(Icons.emoji_events_outlined),
                        alignLabelWithHint: true,
                      ),
                    ),
                  ]),
                  const SizedBox(height: 16),
                  _Section(title: 'Tickets', children: [
                    TextFormField(
                      controller: _ticketsCtrl,
                      keyboardType: TextInputType.number,
                      inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                      decoration: const InputDecoration(
                        labelText: 'Number of Tickets *',
                        hintText: 'e.g. 500',
                        prefixIcon: Icon(Icons.format_list_numbered),
                      ),
                      validator: (v) {
                        if (v == null || v.trim().isEmpty) return 'Required';
                        final n = int.tryParse(v.trim());
                        if (n == null || n < 1) return 'Must be at least 1';
                        if (n > 10000) return 'Max 10,000 tickets';
                        return null;
                      },
                    ),
                    const SizedBox(height: 16),
                    Row(children: [
                      SizedBox(
                        width: 120,
                        child: DropdownButtonFormField<String>(
                          value: _currency,
                          decoration:
                              const InputDecoration(labelText: 'Currency'),
                          items: _currencies
                              .map((c) =>
                                  DropdownMenuItem(value: c, child: Text(c)))
                              .toList(),
                          onChanged: (v) => setState(() => _currency = v!),
                        ),
                      ),
                      const SizedBox(width: 16),
                      Expanded(
                        child: TextFormField(
                          controller: _priceCtrl,
                          keyboardType: const TextInputType.numberWithOptions(
                              decimal: true),
                          inputFormatters: [
                            FilteringTextInputFormatter.allow(
                                RegExp(r'^\d+\.?\d{0,2}')),
                          ],
                          decoration: const InputDecoration(
                            labelText: 'Price per Ticket *',
                            hintText: 'e.g. 10.00',
                            prefixIcon: Icon(Icons.attach_money),
                          ),
                          validator: (v) {
                            if (v == null || v.trim().isEmpty) return 'Required';
                            final n = double.tryParse(v.trim());
                            if (n == null || n <= 0) return 'Must be > 0';
                            return null;
                          },
                        ),
                      ),
                    ]),
                  ]),
                  const SizedBox(height: 24),
                  _loading
                      ? const Center(child: CircularProgressIndicator())
                      : FilledButton.icon(
                          onPressed: _submit,
                          icon: const Icon(Icons.rocket_launch),
                          label: const Text('Create Raffle & Generate Tickets'),
                        ),
                  const SizedBox(height: 24),
                ],
              ),
            ),
          ),
        ),
      ),
    );
  }
}

class _Section extends StatelessWidget {
  final String title;
  final List<Widget> children;

  const _Section({required this.title, required this.children});

  @override
  Widget build(BuildContext context) {
    final cs = Theme.of(context).colorScheme;
    return Card(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(title,
                style: Theme.of(context).textTheme.titleSmall?.copyWith(
                    color: cs.primary, fontWeight: FontWeight.bold)),
            const SizedBox(height: 16),
            ...children,
          ],
        ),
      ),
    );
  }
}
