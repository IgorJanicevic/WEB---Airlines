$(document).ready(function () {
    // Provera da li postoji prijavljeni korisnik u sesiji
    let currentUser = sessionStorage.getItem('currentUser');
    let rezStatus = 0;
    if (currentUser) {
        currentUser = JSON.parse(currentUser);

        $('#navbarDropdown').html(`MyProfile (${currentUser.KorisnickoIme})`);


        if (currentUser.TipKorisnika == 0) {
            let dropdownMenu = `
        <div class="dropdown mr-3">
            <button class="btn btn-secondary dropdown-toggle" type="button" id="statusDropdown" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">
                Status
            </button>
            <div class="dropdown-menu dropdown-menu-right" aria-labelledby="statusDropdown">
                <a class="dropdown-item" href="#" data-status="active" id="activeFlightsLink">Aktivni letovi</a>
                <a class="dropdown-item" href="#" data-status="cancelled" id="cancelledFlightsLink">Otkazani letovi</a>
                <a class="dropdown-item" href="#" data-status="finished" id="finishedFlightsLink">Završeni letovi</a>
            </div>
        </div>
    `;
            $("#searchForm").append(dropdownMenu);

            // Event listener za prikaz aktivnih letova
            $('#activeFlightsLink').click(function (event) {
                event.preventDefault();
                rezStatus = 0;
                loadFlights(0);
            });
            $('#finishedFlightsLink').click(function (event) {
                event.preventDefault();
                rezStatus = 2;
                loadUserFlights(2);
            });
            $('#cancelledFlightsLink').click(function (event) {
                event.preventDefault();
                rezStatus = 1;
                loadUserFlights(1);
            });

            $('#loginLink').hide();


        } else {
            let formHtml = `
            <h4>Dodaj aviokompaniju</h4>
                 <form id="aviokompanijaForm" class="form-inline">
                <label for="naziv" class="sr-only">Naziv</label>
                <input type="text" class="form-control mb-2 mr-sm-2" id="naziv" placeholder="Naziv aviokompanije" required>

                <label for="adresa" class="sr-only">Adresa</label>
                <input type="text" class="form-control mb-2 mr-sm-2" id="adresa" placeholder="Adresa aviokompanije" required>

                <label for="kontakt" class="sr-only">Kontakt Informacije</label>
                <input type="text" class="form-control mb-2 mr-sm-2" id="kontakt" placeholder="Kontakt informacije" required>

                <button type="submit" class="btn btn-primary mb-2">Dodaj</button>
                </form>
            `;
            $("#aviokompanijaAdd").append(formHtml);

            // Event listener za submit forme za dodavanje aviokompanije
            $('#aviokompanijaForm').submit(function (event) {
                event.preventDefault();
                let naziv = $('#naziv').val();
                let adresa = $('#adresa').val();
                let kontaktInformacije = $('#kontakt').val();


                // Primer AJAX poziva za dodavanje aviokompanije
                $.ajax({
                    type: 'POST',
                    url: '/api/aviokompanije/add',
                    data: {
                        naziv: naziv,
                        adresa: adresa,
                        kontaktInformacije: kontaktInformacije,
                        letovi: [],
                        recenzije:[]
                    },
                    success: function (response) {
                        alert('Aviokompanija uspešno dodata!');
                        //window.location.href = "Index.html";
                    },
                    error: function (xhr, status, error) {
                        console.error('Greška prilikom dodavanja aviokompanije:', error);
                        alert('Greška prilikom dodavanja aviokompanije: ' + error);
                    }
                });

                // Resetovanje polja forme nakon uspesnog dodavanja
                $('#naziv').val('');
                $('#adresa').val('');
                $('#kontakt').val('');
            });

            $('#aviokompanijeLink').show();
        }

        if (currentUser.TipKorisnika == 1) { // Korisnik je administrator
            $('#recenzije-rezervacijeLink').show();
        }

       

    } else {
        // Ako nije prijavljen korisnik, sakrijemo MyProfile i Logout
        $('.dropdown-item[href="profile.html"]').hide();
        $('.dropdown-item#logoutLink').hide();
    }

    // Logout funkcionalnost
    $('.dropdown-item#logoutLink').click(function (event) {
        event.preventDefault();
        // Brisanje trenutnog korisnika iz sesije
        sessionStorage.removeItem('currentUser');

        // Redirekcija na login.html
        window.location.href = 'index.html';
    });

    // Event listener za pretragu letova
    $('#searchForm').submit(function (event) {
        event.preventDefault();
        if (rezStatus == 0) {
            loadFlights(rezStatus);
        } else {
            loadUserFlights(rezStatus);
        }
    });

    // Event listener za sortiranje letova
    $('#sortBtn').click(function () {
        let $sortBtn = $(this);
        let sortAsc = $sortBtn.data('sort') === 'asc';

        sortFlights(sortAsc);

        if (sortAsc) {
            $sortBtn.data('sort', 'desc');
        } else {
            $sortBtn.data('sort', 'asc');
        }
    });


    // Postavljanje click event handlera za flight-link
    $(document).on('click', '.flight-link', function (event) {
        event.preventDefault(); 

        // Dobijamo LetId iz roditeljskog <tr> elementa
        let params = $(this).closest('tr').attr('id'); 
        let [letIdPart, statusPart] = params.split('&'); 
        let letId = letIdPart; // 0
        let letStatus = statusPart.split('=')[1]; // 3       


        // Generisemo URL za rezervaciju leta
        let reservationUrl = `rezervisiLet.html?LetId=${letId}`;
        let reviewUrl = `kreirajRecenziju.html?LetId=${letId}`;

        // Preusmeravamo korisnika na odgovarajucu stranicu
        let currentUser = sessionStorage.getItem('currentUser');
        if (currentUser) {
            currentUser = JSON.parse(currentUser);

            if (currentUser.TipKorisnika == 0) {
                if (letStatus == 0) {
                    window.location.href = reservationUrl;
                } else if (letStatus == 1) {

                } else if (letStatus == 2) {
                    window.location.href = reviewUrl;
                }
            } else {
            }
        } else {
            
                window.location.href = "login.html";
            
        }
    });

    // Učitavanje letova pri inicijalnom učitavanju stranice
    loadFlights(0);
});

function loadUserFlights(rezStatus) {
    // Provera da li postoji prijavljeni korisnik u sesiji
    let currentUser = sessionStorage.getItem('currentUser');

    if (currentUser) {
        currentUser = JSON.parse(currentUser);


        // Prvo dohvatanje azuriranog korisnika
        getUserByUsername(currentUser.KorisnickoIme)
            .then(updatedUser => {
                currentUser = updatedUser;
                sessionStorage.setItem('currentUser', JSON.stringify(currentUser));

                return fetch('/api/rezervacije');
            })
            .then(response => response.json())
            .then(rezervacije => {
                let aviokompanija = $('#aviokompanija').val().toLowerCase();
                let polaznaDestinacija = $('#polaznaDestinacija').val().toLowerCase();
                let odredisnaDestinacija = $('#odredisnaDestinacija').val().toLowerCase();
                let datumPolaska = $('#datumPolaska').val();
                let datumDolaska = $('#datumDolaska').val();

                let rows = '';

                // Filtriranje rezervacija koje pripadaju trenutnom korisniku
                rezervacije.forEach(rez => {
                    if (currentUser.ListaRezervacija.includes(rez.RezervacijaId)) {
                        let letObj = rez.Let;
                        let datumPolaskaObj = new Date(letObj.DatumVremePolaska);
                        let datumDolaskaObj = new Date(letObj.DatumVremeDolaska);

                        // Provera za prikaz svih letova ako nijedan filter nije unet
                        if (!aviokompanija && !polaznaDestinacija && !odredisnaDestinacija && !datumPolaska && !datumDolaska && letObj.Status === rezStatus) {
                            rows += `<tr class="flight-link" id="${letObj.LetId}&status=${letObj.Status}">
                                        <td>${letObj.Aviokompanija}</td>
                                        <td>${letObj.PolaznaDestinacija}</td>
                                        <td>${letObj.OdredisnaDestinacija}</td>
                                        <td>${formatDate(datumPolaskaObj)}</td>
                                        <td>${formatDate(datumDolaskaObj)}</td>
                                        <td>${letObj.BrojSlobodnihMesta}</td>
                                        <td>${letObj.BrojZauzetihMesta}</td>
                                        <td>${letObj.Cena}</td>
                                    </tr>`;
                        } else {
                            let isMatch = true;

                            // Provera da li let odgovara filterima
                            if (aviokompanija && !letObj.Aviokompanija.toLowerCase().includes(aviokompanija)) {
                                isMatch = false;
                            }
                            if (polaznaDestinacija && !letObj.PolaznaDestinacija.toLowerCase().includes(polaznaDestinacija)) {
                                isMatch = false;
                            }
                            if (odredisnaDestinacija && !letObj.OdredisnaDestinacija.toLowerCase().includes(odredisnaDestinacija)) {
                                isMatch = false;
                            }
                            if (datumPolaska && !letObj.DatumVremePolaska.includes(datumPolaska)) {
                                isMatch = false;
                            }
                            if (datumDolaska && !letObj.DatumVremeDolaska.includes(datumDolaska)) {
                                isMatch = false;
                            }

                            if (isMatch && letObj.Status === rezStatus) {
                                rows += `<tr class="flight-link" id="${letObj.LetId}&status=${letObj.Status}">
                                            <td>${letObj.Aviokompanija}</td>
                                            <td>${letObj.PolaznaDestinacija}</td>
                                            <td>${letObj.OdredisnaDestinacija}</td>
                                            <td>${formatDate(datumPolaskaObj)}</td>
                                            <td>${formatDate(datumDolaskaObj)}</td>
                                            <td>${letObj.BrojSlobodnihMesta}</td>
                                            <td>${letObj.BrojZauzetihMesta}</td>
                                            <td>${letObj.Cena}</td>
                                        </tr>`;
                            }
                        }
                    }
                });

                $('#bodyF').html(rows);
                $('#aviokompanija').val('');
                $('#polaznaDestinacija').val('');
                $('#odredisnaDestinacija').val('');
                $('#datumPolaska').val('');
                $('#datumDolaska').val('');
            })
            .catch(error => {
                console.error('Greška prilikom dohvatanja rezervacija:', error);
                alert("Greška prilikom dohvatanja rezervacija: " + error);
            });
    } else {
        console.error('Nije prijavljen korisnik.');
        alert("Nije prijavljen korisnik.");
    }
}

function loadFlights(rezStatus) {
    let aviokompanija = $('#aviokompanija').val().toLowerCase();
    let polaznaDestinacija = $('#polaznaDestinacija').val().toLowerCase();
    let odredisnaDestinacija = $('#odredisnaDestinacija').val().toLowerCase();
    let datumPolaska = $('#datumPolaska').val();
    let datumDolaska = $('#datumDolaska').val();
    let statusLeta = '';
    let letovi;

    $.ajax({
        type: 'GET',
        url: '/api/letovi',
        data: {
            aviokompanija: aviokompanija,
            polaznaDestinacija: polaznaDestinacija,
            odredisnaDestinacija: odredisnaDestinacija,
            DatumVremePolaska: datumPolaska,
            DatumVremeDolaska: datumDolaska,
        },
        success: function (data) {
            let rows = '';
            letovi = data;

            letovi.forEach(function (let) {
                let datumPolaskaObj = new Date(let.DatumVremePolaska);
                let datumDolaskaObj = new Date(let.DatumVremeDolaska);

                // Provera za prikaz svih letova ako nijedan filter nije unet
                if (!aviokompanija && !polaznaDestinacija && !odredisnaDestinacija && !datumPolaska && !datumDolaska && let.Status == 0) {
                    rows += `<tr  class="flight-link" id="${let.LetId}&status=${let.Status}">
                            <td>${let.Aviokompanija}</td>
                            <td>${let.PolaznaDestinacija}</td>
                            <td>${let.OdredisnaDestinacija}</td>
                            <td>${formatDate(datumPolaskaObj)}</td>
                            <td>${formatDate(datumDolaskaObj)}</td>
                            <td>${let.BrojSlobodnihMesta}</td>
                            <td>${let.BrojZauzetihMesta}</td>
                            <td>${let.Cena}</td>
                        </tr>`;
                } else {
                    let isMatch = true;

                    // Provera da li let odgovara filterima
                    if (aviokompanija && !let.Aviokompanija.toLowerCase().includes(aviokompanija)) {
                        isMatch = false;
                    }
                    if (polaznaDestinacija && !let.PolaznaDestinacija.toLowerCase().includes(polaznaDestinacija)) {
                        isMatch = false;
                    }
                    if (odredisnaDestinacija && !let.OdredisnaDestinacija.toLowerCase().includes(odredisnaDestinacija)) {
                        isMatch = false;
                    }
                    if (datumPolaska && !let.DatumVremePolaska.includes(datumPolaska)) {
                        isMatch = false;
                    }
                    if (datumDolaska && !let.DatumVremeDolaska.includes(datumDolaska)) {
                        isMatch = false;
                    }

                    if (isMatch && let.Status == 0) {
                        rows += `<tr  class="flight-link" id="${let.LetId}&status=${let.Status}">
                                <td>${let.Aviokompanija}</td>
                                <td>${let.PolaznaDestinacija}</td>
                                <td>${let.OdredisnaDestinacija}</td>
                                <td>${formatDate(datumPolaskaObj)}</td>
                                <td>${formatDate(datumDolaskaObj)}</td>
                                <td>${let.BrojSlobodnihMesta}</td>
                                <td>${let.BrojZauzetihMesta}</td>
                                <td>${let.Cena}</td>
                            </tr>`;
                    }
                }
            });

            $('#bodyF').html(rows);
            $('#aviokompanija').val('');
            $('#polaznaDestinacija').val('');
            $('#odredisnaDestinacija').val('');
            $('#datumPolaska').val('');
            $('#datumDolaska').val('');
        },
        error: function (xhr, status, error) {
            console.error("Error:", xhr.responseText);
        }
    });
}

   
function getUserByUsername(username) {
    return fetch(`/api/korisnici/${username}`)
        .then(response => {
            if (!response.ok) {
                throw new Error('Greška prilikom dohvatanja korisnika');
            }
            return response.json();
        });
}


function formatDate(date) {
    let day = date.getDate();
    let month = date.getMonth() + 1;
    let year = date.getFullYear();
    let hours = date.getHours();
    let minutes = date.getMinutes();

    // Dodajemo nulu ispred meseca, dana, minuta i sati ako su manji od 10
    if (month < 10) {
        month = `0${month}`;
    }
    if (day < 10) {
        day = `0${day}`;
    }
    if (hours < 10) {
        hours = `0${hours}`;
    }
    if (minutes < 10) {
        minutes = `0${minutes}`;
    }

    return `${day}.${month}.${year} ${hours}:${minutes}`;
}

function sortFlights(ascending) {
    let tableRows = $('#bodyF tr').get();

    tableRows.sort(function (rowA, rowB) {
        let priceA = parseFloat($(rowA).find('td').eq(7).text()); // 7 je index kolone sa cenom
        let priceB = parseFloat($(rowB).find('td').eq(7).text());

        if (ascending) {
            return priceA - priceB;
        } else {
            return priceB - priceA;
        }
    });

    $.each(tableRows, function (index, row) {
        $('#bodyF').append(row);
    });
}
