(function () {
    const hostScheme = "appbridge://";
    const defaultCenter = [10.7605, 106.7002];
    const defaultTileUrl = "https://tile.openstreetmap.org/{z}/{x}/{y}.png";
    const defaultTileAttribution = "&copy; OpenStreetMap contributors";
    const colors = {
        user: "#1d8cf8",
        poi: "#dc2626",
        nearest: "#f59e0b",
        route: "#0d6efd"
    };

    const state = {
        map: null,
        tileLayer: null,
        tileKey: "",
        poiLayer: null,
        routeLayer: null,
        userLayer: null,
        routeRequestId: 0,
        poiMarkers: {},
        activePoiId: null,
        isRenderingMapState: false
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

        state.routeLayer = L.layerGroup().addTo(state.map);
        state.poiLayer = L.layerGroup().addTo(state.map);
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
        state.routeLayer.clearLayers();
        state.userLayer.clearLayers();
        state.poiMarkers = {};
    }

    function openPoiPopup(poiId) {
        const marker = state.poiMarkers[String(poiId)];
        if (!marker) {
            return false;
        }

        state.activePoiId = poiId;
        marker.openPopup();
        return true;
    }

    function focusPoi(poiId) {
        const marker = state.poiMarkers[String(poiId)];
        if (!marker) {
            return false;
        }

        const latLng = marker.getLatLng();
        const nextZoom = Math.max(state.map.getZoom() || 0, 17);
        state.map.setView(latLng, nextZoom, {
            animate: true
        });

        return true;
    }

    function escapeHtml(value) {
        return String(value || "").replace(/[&<>"']/g, function (char) {
            return {
                "&": "&amp;",
                "<": "&lt;",
                ">": "&gt;",
                "\"": "&quot;",
                "'": "&#39;"
            }[char];
        });
    }

    function formatPopupText(value) {
        const trimmed = String(value || "").trim();
        if (!trimmed) {
            return "";
        }

        return escapeHtml(trimmed).replace(/\r?\n/g, "<br />");
    }

    function buildPoiPopupHtml(poi) {
        const title = escapeHtml(poi.ten);
        const description = formatPopupText(poi.moTa);
        const imageUrl = typeof poi.imageUrl === "string" ? poi.imageUrl.trim() : "";

        const imageHtml = imageUrl
            ? "<img class=\"poi-popup__image\" src=\"" + escapeHtml(imageUrl) + "\" alt=\"" + title + "\" />"
            : "";
        const descriptionHtml = description
            ? "<div class=\"poi-popup__description\">" + description + "</div>"
            : "";

        return "<div class=\"poi-popup\">" +
            "<div class=\"poi-popup__title\">" + title + "</div>" +
            imageHtml +
            descriptionHtml +
            "</div>";
    }

    function getRouteEndpoints(route) {
        if (!route) {
            return null;
        }

        const start = route.origin || (route.points && route.points[0]);
        const end = route.destination || (route.points && route.points[route.points.length - 1]);

        if (!start || !end) {
            return null;
        }

        return {
            start: start,
            end: end
        };
    }

    function renderPois(pois, popupPoiId) {
        const activePoiId = popupPoiId === null || popupPoiId === undefined
            ? null
            : String(popupPoiId);
        const priorityMarkers = [];

        (pois || []).forEach(function (poi) {
            const isActive = (activePoiId !== null && activePoiId === String(poi.id)) || !!poi.isTracking;
            const isNearest = !!poi.isNearest;
            const markerColor = isNearest ? colors.nearest : colors.poi;
            const markerRadius = isNearest ? 7 : isActive ? 6 : 5;

            if (isNearest || isActive) {
                const geofenceColor = isNearest ? colors.nearest : colors.poi;
                L.circle([poi.lat, poi.lng], {
                    radius: Math.max(poi.banKinh || 0, 20),
                    color: geofenceColor,
                    opacity: 0.65,
                    weight: 1.25,
                    fillColor: geofenceColor,
                    fillOpacity: 0.12,
                    interactive: false
                }).addTo(state.poiLayer);
            }

            if (isNearest) {
                L.circleMarker([poi.lat, poi.lng], {
                    radius: 12,
                    color: colors.nearest,
                    weight: 2,
                    opacity: 0.22,
                    fillColor: colors.nearest,
                    fillOpacity: 0.12,
                    interactive: false
                }).addTo(state.poiLayer);
            }

            const marker = L.circleMarker([poi.lat, poi.lng], {
                radius: markerRadius,
                color: "#ffffff",
                weight: isNearest ? 1.75 : 1.25,
                fillColor: markerColor,
                fillOpacity: 0.95
            });

            marker.bindTooltip(poi.ten, {
                className: "poi-tooltip",
                direction: "top",
                offset: [0, -10],
                opacity: 1
            });

            marker.bindPopup(buildPoiPopupHtml(poi), {
                className: "poi-popup-shell",
                maxWidth: 280,
                autoPanPadding: [24, 24]
            });

            marker.on("popupopen", function () {
                state.activePoiId = poi.id;
            });

            marker.on("popupclose", function () {
                if (state.activePoiId === poi.id) {
                    state.activePoiId = null;
                }

                if (!state.isRenderingMapState) {
                    notifyHost("poi-popup-close", { poiId: String(poi.id) });
                }
            });

            marker.on("click", function () {
                openPoiPopup(poi.id);
                notifyHost("poi-click", { poiId: String(poi.id) });
            });

            marker.addTo(state.poiLayer);
            state.poiMarkers[String(poi.id)] = marker;

            if (isNearest || isActive) {
                priorityMarkers.push(marker);
            }
        });

        priorityMarkers.forEach(function (marker) {
            marker.bringToFront();
        });

        if (popupPoiId !== null && popupPoiId !== undefined) {
            openPoiPopup(popupPoiId);
        }
    }

    function drawStraightLine(start, end, isTemporary) {
        state.routeLayer.clearLayers();

        const latLngs = [[start.lat, start.lng], [end.lat, end.lng]];
        const opacity = isTemporary ? 0.45 : 0.7;

        L.polyline(latLngs, {
            color: colors.route,
            weight: 3,
            opacity: opacity,
            lineCap: "round",
            lineJoin: "round",
            smoothFactor: 1.2,
            dashArray: isTemporary ? "6, 8" : null,
            interactive: false
        }).addTo(state.routeLayer);
    }

    async function renderRoute(route) {
        const endpoints = getRouteEndpoints(route);
        if (!endpoints) {
            state.routeRequestId += 1;
            return;
        }

        const start = endpoints.start;
        const end = endpoints.end;
        const myRequestId = ++state.routeRequestId;

        drawStraightLine(start, end, true);

        try {
            const url = "https://router.project-osrm.org/route/v1/driving/" +
                start.lng + "," + start.lat + ";" + end.lng + "," + end.lat +
                "?overview=full&geometries=geojson";
            const response = await fetch(url, { signal: AbortSignal.timeout(5000) });

            if (myRequestId !== state.routeRequestId) {
                return;
            }

            if (!response.ok) {
                throw new Error("OSRM HTTP " + response.status);
            }

            const data = await response.json();

            if (myRequestId !== state.routeRequestId) {
                return;
            }

            if (!data.routes || data.routes.length === 0) {
                throw new Error("No route found");
            }

            state.routeLayer.clearLayers();

            const coords = data.routes[0].geometry.coordinates;
            const latLngs = coords.map(function (coord) {
                return [coord[1], coord[0]];
            });

            L.polyline(latLngs, {
                color: colors.route,
                weight: 3,
                opacity: 0.7,
                lineCap: "round",
                lineJoin: "round",
                smoothFactor: 1.2,
                interactive: false
            }).addTo(state.routeLayer);
        } catch (error) {
            if (myRequestId === state.routeRequestId) {
                drawStraightLine(start, end, false);
            }
        }
    }

    function renderUser(userLocation) {
        if (!userLocation) {
            return;
        }

        L.circle([userLocation.lat, userLocation.lng], {
            radius: 25,
            color: colors.user,
            opacity: 0.28,
            weight: 1,
            fillColor: colors.user,
            fillOpacity: 0.16,
            interactive: false
        }).addTo(state.userLayer);

        L.circleMarker([userLocation.lat, userLocation.lng], {
            radius: 5,
            color: "#ffffff",
            weight: 2,
            fillColor: colors.user,
            fillOpacity: 1,
            interactive: false
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
        const renderedRouteBounds = state.routeLayer && typeof state.routeLayer.getBounds === "function"
            ? state.routeLayer.getBounds()
            : null;
        if (renderedRouteBounds && renderedRouteBounds.isValid()) {
            state.map.fitBounds(renderedRouteBounds.pad(0.2), {
                animate: true,
                padding: [36, 36]
            });

            return true;
        }

        const endpoints = getRouteEndpoints(route);
        if (!endpoints) {
            return false;
        }

        const leafletBounds = L.latLngBounds(
            [
                [endpoints.start.lat, endpoints.start.lng],
                [endpoints.end.lat, endpoints.end.lng]
            ]
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
        if (mapState.focusPoiId !== null && mapState.focusPoiId !== undefined && focusPoi(mapState.focusPoiId)) {
            return;
        }

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

    async function setMapState(mapState) {
        state.isRenderingMapState = true;
        state.activePoiId = mapState.popupPoiId === undefined ? null : mapState.popupPoiId;

        try {
            ensureMap();
            ensureTileLayer(mapState.tileUrlTemplate, mapState.tileAttribution);
            clearDynamicLayers();
            renderPois(mapState.pois, mapState.popupPoiId);
            await renderRoute(mapState.route);
            renderUser(mapState.userLocation);
            applyViewport(mapState);
        } finally {
            state.isRenderingMapState = false;
        }
    }

    document.addEventListener("DOMContentLoaded", ensureMap);

    window.mapBridge = {
        setMapState: setMapState
    };
})();
