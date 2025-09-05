(function (global) {
	"use strict";

	function jsonOrThrow(res) {
		if (!res.ok) {
			return res.text().then(t => {
				throw new Error(`HTTP ${res.status}: ${t || res.statusText}`);
			});
		}
		return res.json();
	}

	const AddressApi = {
		reverseGeocode(lat, lng) {
			return fetch("/api/address/reverse-geocode", {
				method: "POST",
				headers: { "Content-Type": "application/json" },
				body: JSON.stringify({ lat, lng })
			}).then(jsonOrThrow);
		},

		geocodeText(query) {
			return fetch("/api/address/geocode-text", {
				method: "POST",
				headers: { "Content-Type": "application/json" },
				body: JSON.stringify({ query })
			}).then(jsonOrThrow);
		},

		placeDetails(placeId) {
			return fetch("/api/address/place-details", {
				method: "POST",
				headers: { "Content-Type": "application/json" },
				body: JSON.stringify({ placeId })
			}).then(jsonOrThrow);
		},

		autocomplete(query) {
			return fetch("/api/address/autocomplete", {
				method: "POST",
				headers: { "Content-Type": "application/json" },
				body: JSON.stringify({ query })
			}).then(jsonOrThrow);
		}
	};

	global.MapService = { AddressApi };
})(window);
