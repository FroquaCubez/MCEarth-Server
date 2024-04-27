var map = L.map('map').setView([0, 0], 2);
L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
}).addTo(map);

function mostrarTocables() {
    fetch('1/api/v1.1/locations/0/0')
        .then(response => response.json())
        .then(data => {
            const tocables = data.result.activeLocations.filter(location => location.type === 'Tappable');

            tocables.forEach(tocable => {
                const { id, coordinate, spawnTime, expirationTime, type, icon, metadata, tappableMetadata } = tocable;
                const { latitude, longitude } = coordinate;
                const iconName = icon.replace('genoa:', '');
                const iconUrl = `${window.location.origin}/images/tappables/icons/${iconName}.png`;

                const popupContent = `
                            <b>Tocable</b><br>
                            ID: ${id}<br>
                            Latitude: ${latitude}<br>
                            Longitude: ${longitude}<br>
                            Spawn Time: ${spawnTime}<br>
                            Expiration Time: ${expirationTime}<br>
                            Type: ${type}<br>
                            Icon: ${iconName}<br>
                            Reward ID: ${metadata.rewardId}<br>
                            Rarity: ${metadata.rarity}<br>
                            Tappable Metadata Rarity: ${tappableMetadata.rarity}<br>
                        `;

                L.marker([latitude, longitude], { icon: L.icon({ iconUrl, iconSize: [25, 25] }) })
                    .addTo(map)
                    .bindPopup(popupContent);
            });
        })
        .catch(error => {
            console.error('Error al obtener los datos de los tocables:', error);
        });
}