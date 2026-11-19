# Bugs I found

| Rule | Starter problem | What I changed |
|---|---|---|
| R5–R6 | BookingService didn t check past start times or booking length | added both checks to Booking.Create and used TimeProvider. |
| R7 | the overlap check didn t handle bookings that touch corectly | addded tests and changed the check so touching bookings are allowed |
| R8 | cancelling didn t properly  an already cancelled booking properly | added Booking.Cancel and return a conflict for a second one |
