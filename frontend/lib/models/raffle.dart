class Raffle {
  final String id;
  final String name;
  final String? description;
  final String? organisation;
  final String? prizes;
  final int numberOfTickets;
  final double ticketPrice;
  final String currency;
  final DateTime createdAt;
  final int claimedTickets;
  final List<Ticket>? tickets;

  const Raffle({
    required this.id,
    required this.name,
    this.description,
    this.organisation,
    this.prizes,
    required this.numberOfTickets,
    required this.ticketPrice,
    required this.currency,
    required this.createdAt,
    required this.claimedTickets,
    this.tickets,
  });

  factory Raffle.fromJson(Map<String, dynamic> json) => Raffle(
        id: json['id'],
        name: json['name'],
        description: json['description'],
        organisation: json['organisation'],
        prizes: json['prizes'],
        numberOfTickets: json['numberOfTickets'],
        ticketPrice: (json['ticketPrice'] as num).toDouble(),
        currency: json['currency'],
        createdAt: DateTime.parse(json['createdAt']),
        claimedTickets: json['claimedTickets'] ?? 0,
        tickets: json['tickets'] != null
            ? (json['tickets'] as List).map((t) => Ticket.fromJson(t)).toList()
            : null,
      );
}

class Ticket {
  final String id;
  final int ticketNumber;
  final bool isClaimed;
  final String? claimedName;
  final String? claimedSurname;
  final String? claimedMobile;
  final String? claimedEmail;
  final String? purchasedFrom;
  final DateTime? claimedAt;
  final Raffle? raffle;

  const Ticket({
    required this.id,
    required this.ticketNumber,
    required this.isClaimed,
    this.claimedName,
    this.claimedSurname,
    this.claimedMobile,
    this.claimedEmail,
    this.purchasedFrom,
    this.claimedAt,
    this.raffle,
  });

  factory Ticket.fromJson(Map<String, dynamic> json) => Ticket(
        id: json['id'],
        ticketNumber: json['ticketNumber'],
        isClaimed: json['isClaimed'],
        claimedName: json['claimedName'],
        claimedSurname: json['claimedSurname'],
        claimedMobile: json['claimedMobile'],
        claimedEmail: json['claimedEmail'],
        purchasedFrom: json['purchasedFrom'],
        claimedAt: json['claimedAt'] != null ? DateTime.parse(json['claimedAt']) : null,
        raffle: json['raffle'] != null ? Raffle.fromJson(json['raffle']) : null,
      );
}
