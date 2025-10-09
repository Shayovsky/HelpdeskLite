Sklonuj repozytorium:

git clone <repo-url>
cd HelpdeskLite


Przygotuj bazę danych
Upewnij się, że w pliku appsettings.json masz poprawny connection string do SQL Server:

"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=HelpdeskLiteDb;Trusted_Connection=True;MultipleActiveResultSets=true"
}


Wykonaj migracje (utworzenie tabel w bazie):

dotnet ef database update


Utworzone zostaną m.in. tabele:

Tickets (Id, Title, Description, Status, Priority, AssignedToId, CreatedAt, ResolvedAt, ClosedBySystem)

Comments (Id, Content, CreatedAt, TicketId, UserId)

Attachments (Id, FileName, FilePath, TicketId)

Tabele Identity: AspNetUsers, AspNetRoles, AspNetUserRoles itd.

Uruchom aplikację:

dotnet run

1. Konta testowe

Przy pierwszym uruchomieniu system tworzy role: Admin, Agent, User.
Możesz samodzielnie dodać użytkownika do roli np. w SQL:

INSERT INTO AspNetUserRoles (RoleId, UserId)
VALUES ('<RoleId>', '<UserId>');

Przykładowe konta
Rola	    Login	                    Hasło

Agent	    mateuszsatlawa981@gmail.com zaq1@WSX

User	    12345@gmail.com             ZAQ1@wsx


(jeżeli nie utworzyłeś tych kont ręcznie – zarejestruj użytkownika i przypisz mu rolę w bazie).

2. Funkcjonalności
- Tickets

Tworzenie zgłoszeń (Title, Description, Priority, załączniki).

Statusy: New, InProgress, Resolved, Closed.

Priorytety: Low, Normal, High.

Możliwość przypisania zgłoszenia do Agenta.

- Comments

Dodawanie komentarzy do zgłoszeń.

Historia z autorem i datą.

- Attachments

Dodawanie i pobieranie załączników.

- Dashboard (dla Admin/Agent)

Podsumowanie liczby zgłoszeń w statusach.

Liczba nowych zgłoszeń dzisiaj.

- Background Service

Automatyczne zamykanie zgłoszeń po 7 dniach w statusie Resolved.

W szczegółach zgłoszenia pojawia się komunikat:

This ticket was automatically closed by the system after 7 days in resolved state.
