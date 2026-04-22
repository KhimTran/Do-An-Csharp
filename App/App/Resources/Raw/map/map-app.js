(function () {
    const hostScheme = "appbridge://";
    const defaultCenter = [10.7605, 106.7002];
    const defaultTileUrl = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";
    const defaultTileAttribution = "&copy; OpenStreetMap contributors";

    const state = {
        map: null,
        tileLayer: null,
        tileKey: "",
        poiLayer: null,
        geofenceLayer: null,
        routeLayer: null,
        userLayer: null
    };

    function notifyHost(action, params) {
        const query = params ? new URLSearchParams(params).toString() : "";
        const iframe = document.createElement("iframe");
        iframe.style.display = "none";
        iframe.src = hostScheme + action + (query ? "?" + query : "");
        document.documentElement.appendChild(iframe);

        setTimeout(function () {
            iframe.remove();
        }, 0);
    }

    function ensureMap() {
        if (state.map) {
            return;
        }

        state.map = L.map("map", {
            preferCanvas: true,
            zoomControl: true
        });

        state.map.setView(defaultCenter, 16);

        state.poiLayer = L.layerGroup().addTo(state.map);
        state.geofenceLayer = L.layerGroup().addTo(state.map);
        state.routeLayer = L.layerGroup().addTo(state.map);
        state.userLayer = L.layerGroup().addTo(state.map);

        notifyHost("ready");
    }

    function ensureTileLayer(tileUrl, attribution) {
        const nextKey = [tileUrl || defaultTileUrl, attribution || defaultTileAttribution].join("|");
        if (state.tileLayer && state.tileKey === nextKey) {
            return;
        }

        if (state.tileLayer) {
            state.map.removeLayer(state.tileLayer);
        }

        state.tileLayer = L.tileLayer(tileUrl || defaultTileUrl, {
            attribution: attribution || defaultTileAttribution,
            maxZoom: 19
        });

        state.tileLayer.addTo(state.map);
        state.tileKey = nextKey;
    }

    function clearDynamicLayers() {
        state.poiLayer.clearLayers();
        state.geofenceLayer.clearLayers();
        state.routeLayer.clearLayers();
        state.userLayer.clearLayers();
    }

    function renderPois(pois) {
        (pois || []).forEach(function (poi) {
            const color = poi.isTracking ? "#0d6efd" : poi.isNearest ? "#f59e0b" : "#d92d20";
            const marker = L.circleMarker([poi.lat, poi.lng], {
                radius: poi.isTracking ? 10 : poi.isNearest ? 9 : 8,
                color: "#ffffff",
                weight: 2,
                fillColor: color,
                fillOpacity: 0.95
            });

            marker.bindTooltip(poi.ten, {
                className: "poi-tooltip",
                direction: "top",
                offset: [0, -10],
                opacity: 1
            });

            marker.on("click", function () {
                notifyHost("poi-click", { poiId: String(poi.id) });
            });

            marker.addTo(state.poiLayer);

            L.circle([poi.lat, poi.lng], {
                radius: Math.max(poi.banKinh || 0, 20),
                color: color,
                weight: 2,
                fillColor: color,
                fillOpacity: 0.14
            }).addTo(state.geofenceLayer);
        });
    }

    function renderRoute(route) {
        if (!route || !route.points || route.points.length < 2) {
            return;
        }

        const latLngs = route.points.map(function (point) {
            return [point.lat, point.lng];
        });

        L.polyline(latLngs, {
            color: "#ffffff",
            weight: 10,
            opacity: 0.92,
            lineCap: "round",
            lineJoin: "round"
        }).addTo(state.routeLayer);

        L.polyline(latLngs, {
            color: "#0d6efd",
            weight: 6,
            opacity: 1,
            lineCap: "round",
            lineJoin: "round"
        }).addTo(state.routeLayer);
    }

    function renderUser(userLocation) {
        if (!userLocation) {
            return;
        }

        L.circle([userLocation.lat, userLocation.lng], {
            radius: 14,
            color: "#90caf9",
            weight: 1,
            fillColor: "#42a5f5",
            fillOpacity: 0.2
        }).addTo(state.userLayer);

        L.circleMarker([userLocation.lat, userLocation.lng], {
            radius: 7,
            color: "#ffffff",
            weight: 2,
            fillColor: "#1e88e5",
            fillOpacity: 1
        }).addTo(state.userLayer);
    }

    function fitPois(bounds) {
        if (!bounds) {
            return false;
        }

        if (
            typeof bounds.minLat !== "number" ||
            typeof bounds.minLng !== "number" ||
            typeof bounds.maxLat !== "number" ||
            typeof bounds.maxLng !== "number"
        ) {
            return false;
        }

        const leafletBounds = L.latLngBounds(
            [bounds.minLat, bounds.minLng],
            [bounds.maxLat, bounds.maxLng]
        );

        if (!leafletBounds.isValid()) {
            return false;
        }

        state.map.fitBounds(leafletBounds.pad(0.35), {
            animate: true,
            padding: [28, 28]
        });

        return true;
    }

    function focusRoute(route) {
        if (!route || !route.points || route.points.length < 2) {
            return false;
        }

        const leafletBounds = L.latLngBounds(
            route.points.map(function (point) {
                return [point.lat, point.lng];
            })
        );

        if (!leafletBounds.isValid()) {
            return false;
        }

        state.map.fitBounds(leafletBounds.pad(0.4), {
            animate: true,
            padding: [36, 36]
        });

        return true;
    }

    function followUser(userLocation) {
        if (!userLocation) {
            return false;
        }

        const nextZoom = Math.max(state.map.getZoom() || 0, 16);
        state.map.setView([userLocation.lat, userLocation.lng], nextZoom, {
            animate: true
        });

        return true;
    }

    function applyViewport(mapState) {
        if (mapState.focusOnRoute && focusRoute(mapState.route)) {
            return;
        }

        if (mapState.fitToPois && fitPois(mapState.bounds)) {
            return;
        }

        if (mapState.followUser) {
            followUser(mapState.userLocation);
        }
    }

    function setMapState(mapState) {
        ensureMap();
        ensureTileLayer(mapState.tileUrlTemplate, mapState.tileAttribution);
        clearDynamicLayers();
        renderPois(mapState.pois);
        renderRoute(mapState.route);
        renderUser(mapState.userLocation);
        applyViewport(mapState);
    }

    document.addEventListener("DOMContentLoaded", ensureMap);

    window.mapBridge = {
        setMapState: setMapState
    };
})();
