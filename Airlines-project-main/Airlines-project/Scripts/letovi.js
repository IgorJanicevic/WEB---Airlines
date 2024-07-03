$(document).ready(function () {
    function fetchFlights() {
        $.ajax({
            type: 'GET',
            url: '/api/letovi',
            dataType: 'json',
            success: function (flights) {
                let tbody = $('#flightsTable tbody');
                tbody.empty();
                flights.forEach(function (flight) {
                    let row = `<tr>
                        <td>${flight.Aviokompanija}</td>
                        <td>${flight.PolaznaDestinacija}</td>
                        <td>${flight.OdredisnaDestinacija}</td>
                        <td>${new Date(flight.DatumVremePolaska).toLocaleString()}</td>
                        <td>${new Date(flight.DatumVremeDolaska).toLocaleString()}</td>
                        <td>${flight.BrojSlobodnihMesta}</td>
                        <td>${flight.Cena}</td>
                    </tr>`;
                    tbody.append(row);
                });
            },
            error: function (xhr, textStatus, errorThrown) {
                console.error('Greška prilikom dohvatanja letova:', xhr.responseText);
            }
        });
    }

    fetchFlights(); // Pozovi funkciju za dohvatanje letova prilikom učitavanja stranice
});
