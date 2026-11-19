# Decisions

## 1. A rule I moved

the time and duration checks were in BookingService in the starter, now they
are in Booking.Create in Domain/Bookings/Booking.cs, as they are booking rules,
so they belong in the domain

## 2. `Result` instead of an exception

an occupied room is a normal conflict, not a crash, the endpoint turns it into a 409 response

## 3. Who knows about EF Core?

CampusHubDbContext and the repository classes use EF Core, the domain and
handlers don t

## 4. Request flow

RoomsEndpoints -> CreateBookingEndpoint -> CreateBookingHandler
-> Booking.Create -> BookingRepository -> IUnitOfWork
-> CampusHubDbContext -> SQLite

## 5. What I rejected from AI

i didn't use a generic repository that just forwards DbSet calls

## 6. If I had 30 more minutes
