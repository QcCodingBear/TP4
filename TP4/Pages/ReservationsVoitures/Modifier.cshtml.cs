using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TP4.Services;
using TP4.Models;

namespace TP4.Pages.ReservationsVoitures
{
    public class ModifierModel : PageModel
    {
        [BindProperty]
        public required Reservation Reservation { get; set; }

        public required List<Voiture> ListeDeVoitures { get; set; }

        public required List<Reservation> ListeDeReservations { get; set; }

        [BindProperty]
        public int IdVoiture { get; set; }

        public ActionResult OnGet(int? id)
        {
            if (id == null ||
                GestionReservable.ObtenirReservableParId("Reservation", id.Value) is not Reservation reservation || 
                reservation is null ||
                reservation.ObjetDeLaReservation is null || 
                GestionReservable.ObtenirReservableParId("Voiture", reservation.ObjetDeLaReservation.Id) is not Voiture)
                return NotFound();

            Reservation = reservation;
            IdVoiture = reservation.ObjetDeLaReservation.Id;

            ChargementDePage();

            return Page();
        }

        public IActionResult OnPost()
        {
            ChargementDePage();
            ListeDeReservations = [.. GestionReservable.ObtenirListeReservable("Reservation").Cast<Reservation>()];

            // Verification de l'objet voiture et attribution � la reservation si il n'y a pas d'erreur.
            Voiture voiture = ListeDeVoitures.First(v => v.Id == IdVoiture);
            if (voiture == null)
            {
                ModelState.AddModelError("IdVoiture", "Veuillez s�lectionner une voiture.");
            }
            else
            {
                Reservation.ObjetDeLaReservation = voiture;
                Reservation.PrixJournalier = voiture.PrixJournalier;
                Reservation.Prix = Reservation.PrixJournalier * ((Reservation.DateFin - Reservation.DateDebut).Days + 1);
            }

            if (Reservation.DateDebut < DateTime.Now.Date)
            {
                ModelState.AddModelError("Reservation.DateDebut", "La date de d�but doit �tre sup�rieure ou �gale � la date actuelle.");
            }
            if (Reservation.DateFin < Reservation.DateDebut)
            {
                ModelState.AddModelError("Reservation.DateFin", "La date de fin doit �tre sup�rieure ou �gale � la date de d�but.");
            }

            // Verifie que la voiture n'a pas d�j� �t� r�serv�e sur les dates concern�es
            // ET que ce n'est pas la voiture de la reservation actuelle
            if (ListeDeReservations
                    .Where(r => r.ObjetDeLaReservation.Id == Reservation.ObjetDeLaReservation.Id && r.Id != Reservation.Id)
                    .Any(r => r.DateFin >= Reservation.DateDebut && r.DateDebut <= Reservation.DateFin))
            {
                ModelState.AddModelError("Reservation.ObjetDeLaReservation", "Cette voiture est d�j� r�serv�e pour ces dates.");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            GestionReservable.ModifierReservable(Reservation);
            return RedirectToPage("Index");
        }

        // Recup�ration et attribution des listes.
        private void ChargementDePage()
        {
            ListeDeVoitures = [];
            List<Voiture> ListeAVerifier = [.. GestionReservable.ObtenirListeReservable("Voiture").Cast<Voiture>()];

            int anneeMax = DateTime.Now.Year;
            int anneeMin = anneeMax - 10;

            foreach (var voiture in ListeAVerifier)
            {
                if (voiture.AnneeFabrication > anneeMin)
                {
                    ListeDeVoitures.Add(voiture);
                }
            }
        }
    }
}
