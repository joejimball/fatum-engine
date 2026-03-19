// Mapa de calor KDE para Fatum. Soporta imagen PNG (estilo WPF) o capa Leaflet.heat.
window.fatumHeatmap = {
    map: null,
    heatLayer: null,
    imageOverlay: null,
    pointsLayer: null,
    anomalyLayer: null,

    // Mapa con imagen PNG del heatmap. points: [[lat, lon], ...], showPoints: boolean, anomalyPoints: [[lat, lon], ...]
    // bounds: [minLat, minLon, maxLat, maxLon]
    initWithImage: function (containerId, centerLat, centerLon, imageDataUrl, bounds, points, showPoints, anomalyPoints) {
        if (this.map) {
            this.map.remove();
            this.map = null;
            this.heatLayer = null;
            this.imageOverlay = null;
            this.pointsLayer = null;
            this.anomalyLayer = null;
        }
        var container = document.getElementById(containerId);
        if (!container) return;
        this.map = L.map(containerId).setView([centerLat, centerLon], 13);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap'
        }).addTo(this.map);
        if (imageDataUrl && bounds && bounds.length === 4) {
            var minLat = bounds[0], minLon = bounds[1], maxLat = bounds[2], maxLon = bounds[3];
            var southWest = L.latLng(minLat, minLon);
            var northEast = L.latLng(maxLat, maxLon);
            this.imageOverlay = L.imageOverlay(imageDataUrl, L.latLngBounds(southWest, northEast), { opacity: 0.75 }).addTo(this.map);
            this.map.fitBounds(L.latLngBounds(southWest, northEast), { padding: [20, 20], maxZoom: 15 });
        }
        if (points && points.length > 0) {
            this.pointsLayer = L.layerGroup();
            for (var i = 0; i < points.length; i++) {
                var p = points[i];
                L.circleMarker([p[0], p[1]], {
                    radius: 3,
                    fillColor: '#fff',
                    color: '#333',
                    weight: 0.5,
                    opacity: 0.9,
                    fillOpacity: 0.7
                }).addTo(this.pointsLayer);
            }
            if (showPoints)
                this.pointsLayer.addTo(this.map);
        }
        if (anomalyPoints && anomalyPoints.length > 0) {
            this.anomalyLayer = L.layerGroup();
            var xIcon = L.divIcon({
                className: 'fatum-anomaly-x-marker',
                html: '<span style="color:#000;font-size:20px;font-weight:bold;line-height:20px;display:inline-block;text-shadow:0 0 2px #fff, 0 0 2px #fff;">×</span>',
                iconSize: [24, 24],
                iconAnchor: [12, 12]
            });
            for (var j = 0; j < anomalyPoints.length; j++) {
                var a = anomalyPoints[j];
                L.marker([a[0], a[1]], { icon: xIcon }).addTo(this.anomalyLayer);
            }
            this.anomalyLayer.addTo(this.map);
        }
    },

    setPointsVisible: function (visible) {
        if (!this.map || !this.pointsLayer) return;
        if (visible)
            this.pointsLayer.addTo(this.map);
        else
            this.map.removeLayer(this.pointsLayer);
    },

    init: function (containerId, centerLat, centerLon, heatPoints) {
        if (this.map) {
            this.map.remove();
            this.map = null;
            this.heatLayer = null;
            this.imageOverlay = null;
            this.pointsLayer = null;
            this.anomalyLayer = null;
        }
        var container = document.getElementById(containerId);
        if (!container) return;
        this.map = L.map(containerId).setView([centerLat, centerLon], 13);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            attribution: '© OpenStreetMap'
        }).addTo(this.map);
        if (heatPoints && heatPoints.length > 0) {
            this.heatLayer = L.heatLayer(heatPoints, {
                radius: 25,
                blur: 15,
                maxZoom: 17,
                max: 1,
                gradient: { 0.0: 'blue', 0.33: 'green', 0.66: 'yellow', 1.0: 'red' }
            }).addTo(this.map);
            var minLat = Infinity, maxLat = -Infinity, minLon = Infinity, maxLon = -Infinity;
            for (var i = 0; i < heatPoints.length; i++) {
                var lat = heatPoints[i][0], lon = heatPoints[i][1];
                if (lat < minLat) minLat = lat;
                if (lat > maxLat) maxLat = lat;
                if (lon < minLon) minLon = lon;
                if (lon > maxLon) maxLon = lon;
            }
            if (minLat !== Infinity && maxLat >= minLat && maxLon >= minLon) {
                this.map.fitBounds(L.latLngBounds([[minLat, minLon], [maxLat, maxLon]]), { padding: [20, 20], maxZoom: 15 });
            }
        }
    }
};
