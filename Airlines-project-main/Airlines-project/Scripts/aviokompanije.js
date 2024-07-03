$(document).ready(function () {
    function loadAviokompanije(naziv = '', adresa = '', kontakt = '') {
        $.get('/api/aviokompanije', function (data) {
            const aviokompanijeList = $('#aviokompanijeList');
            aviokompanijeList.empty();

            let currentUser = sessionStorage.getItem('currentUser');
            if (currentUser) {
                currentUser = JSON.parse(currentUser);

                if (currentUser.TipKorisnika == 1) {
                    let pretrazivanje = `
                        <div class="mb-3">
                            <form id="pretragaForm" class="form-inline">
                                <div class="form-group mr-2">
                                    <label for="pretragaNaziv" class="sr-only">Naziv</label>
                                    <input type="text" class="form-control" id="pretragaNaziv" placeholder="Unesite naziv">
                                </div>
                                <div class="form-group mr-2">
                                    <label for="pretragaAdresa" class="sr-only">Adresa</label>
                                    <input type="text" class="form-control" id="pretragaAdresa" placeholder="Unesite adresu">
                                </div>
                                <div class="form-group mr-2">
                                    <label for="pretragaKontakt" class="sr-only">Kontakt</label>
                                    <input type="text" class="form-control" id="pretragaKontakt" placeholder="Unesite kontakt">
                                </div>
                                <button type="submit" class="btn btn-primary">Pretraži</button>
                            </form>
                        </div>
                    `;
                    aviokompanijeList.append(pretrazivanje);

                    // Event listener za submit forme za pretragu
                    $('#pretragaForm').on('submit', function (event) {
                        event.preventDefault(); // Zaustavlja podrazumevano ponašanje forme (submit)

                        const naziv = $('#pretragaNaziv').val().trim();
                        const adresa = $('#pretragaAdresa').val().trim();
                        const kontakt = $('#pretragaKontakt').val().trim();

                        // Ponovo ucitaj aviokompanije sa novim parametrima pretrage
                        loadAviokompanije(naziv, adresa, kontakt);
                    });
                }
            }

            data.forEach(aviokompanija => {
                let prikazi = true;

                if (naziv && !aviokompanija.Naziv.toLowerCase().includes(naziv.toLowerCase())) {
                    prikazi = false;
                }
                if (adresa && !aviokompanija.Adresa.toLowerCase().includes(adresa.toLowerCase())) {
                    prikazi = false;
                }
                if (kontakt && !aviokompanija.KontaktInformacije.toLowerCase().includes(kontakt.toLowerCase())) {
                    prikazi = false;
                }

                if (prikazi) {
                    if (currentUser && currentUser.TipKorisnika == 1) {
                        // Prikaz u obliku tabele za administratora
                        aviokompanijaElement = `
                            <div class="list-group-item avio
                            d-flex justify-content-between align-items-center" data-aviokompanija-id="${aviokompanija.AviokompanijaId}">
                                <span class="avio-link" data-aviokompanija-id="${aviokompanija.AviokompanijaId}">${aviokompanija.Naziv}</span>
                                <span>
                                    <button type="button" class="btn btn-primary btn-sm mr-2 edit-btn" data-aviokompanija-id="${aviokompanija.AviokompanijaId}">Izmeni</button>
                                    <button type="button" class="btn btn-danger btn-sm delete-btn" data-aviokompanija-id="${aviokompanija.AviokompanijaId}">Obriši</button>
                                </span>
                            </div>
                        `;
                    } else {
                        // Prikaz za običnog korisnika (putnika)
                        aviokompanijaElement = `
                            <div class="list-group-item avio-link" data-aviokompanija-id="${aviokompanija.AviokompanijaId}">
                                <h5>${aviokompanija.Naziv}</h5>
                            </div>
                        `;
                    }
                    aviokompanijeList.append(aviokompanijaElement);
                }
            });
        });
    }

    loadAviokompanije();

    // Event listener za klik na avio-link
    $(document).on('click', '.avio-link', function () {
        const aviokompanijaId = $(this).data('aviokompanija-id');

        window.location.href = `aviokompanija.html?id=${aviokompanijaId}`;
    });

    // Event listener za dugme Izmeni 
    $(document).on('click', '.edit-btn', function (event) {
        event.stopPropagation(); // Zaustavlja propagaciju događaja prema roditeljskom elementu
        const aviokompanijaId = $(this).data('aviokompanija-id');

        window.location.href = `izmeni-aviokompaniju.html?id=${aviokompanijaId}`;
    });

    // Event listener za dugme Obrisi
    $(document).on('click', '.delete-btn', function (event) {
        event.stopPropagation(); // Zaustavlja propagaciju događaja prema roditeljskom elementu
        const aviokompanijaId = $(this).data('aviokompanija-id');

        $.ajax({
            type: 'DELETE',
            url: `/api/aviokompanije/${aviokompanijaId}`,
            success: function (response) {
                alert('Aviokompanija uspešno obrisana!');
                window.location.href = "Index.html";
            },
            error: function (xhr, status, error) {
                console.error('Greška prilikom brisanja aviokompanije:', xhr.responseText);
                alert('Nemoguce obrisati aviokompaniju koja ima aktivne letove!');
            }
        });
    });

    // Event listener za submit forme za pretragu
    $(document).on('click', '#pretraziDugme', function (event) {
        const naziv = $('#pretragaNaziv').val().trim();
        const adresa = $('#pretragaAdresa').val().trim();
        const kontakt = $('#pretragaKontakt').val().trim();

        loadAviokompanije(naziv, adresa, kontakt);
        $('#pretragaNaziv').val('');
        $('#pretragaAdresa').val('');
        $('#pretragaKontakt').val('');
       
    });

});
