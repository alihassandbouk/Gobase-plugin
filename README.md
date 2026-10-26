# GoBase for ArcGIS Pro

An ArcGIS Pro add-in over the GoBase catalog. It lists the catalog's
vectors, rasters and documents through the GoBase-Web API, and loads vector
layers into the active map.

Forked from the RASID GoPilot add-in (`upstream` remote). The shell (ribbon
button, side panel, login, credential storage, layer loading) comes from
there; the GoPilot chat and the RASID project pages were removed and stay in
git history for when chat comes back.

## Status

| | |
|---|---|
| Sign in with server URL + API key | built; needs API keys on the backend (GoBase-Web ADR-019) |
| Catalog list: search, type filter, map-view filter, paging | built |
| Vectors: add to map | built: reads `/api/map/assets/{id}/data` for the map view, at most 6 pages of 10,000 features |
| Zoom to an asset | built |
| Rasters (COG) | not yet: waits on the backend's `raster-access` endpoint |
| Documents (PDF) | not yet: `/api/catalog/assets/{id}/pdf` already exists |

## How it fits together

ArcGIS Pro reads `Config.daml` at start-up and adds a **GoBase** tab with
one button. The button opens the side panel. The panel shows one page at a
time:

```
Config.daml                          tab, button, panel (no code)
RibbonButton/ShowGoBaseDockpaneButton.cs   opens the panel
Dockpane/GoBaseDockpaneViewModel.cs  picks the page: Login, Catalog, About
Pages/<Page>/<Page>View.xaml         the page's layout
Pages/<Page>/<Page>ViewModel.cs      the page's data and button actions
Services/                            everything that talks to the server or the map
  ApiClient.cs        HTTP, server URL, the Api-Key header
  GoBaseApiClient.cs  one method per GoBase-Web endpoint
  AuthService.cs      server URL + key, kept in Windows Credential Manager
  VectorLoader.cs     pages through /data, splits by geometry type
  LayerLoader.cs      GeoJSON -> feature class (JSONToFeatures) -> map layer
  MapExtent.cs        the map view as a lon/lat box; zoom to a box
Models/               the JSON shapes the API returns
```

Two things that differ from QGIS plugins:

- **Map work runs on Pro's map thread.** Anything that touches layers or
  geometry is wrapped in `QueuedTask.Run(...)`. Calling it from the UI thread
  throws.
- **A layer holds one geometry type.** Many GoBase vector assets mix points,
  lines and polygons, so the loader writes one layer per type under a group
  named after the asset.

Loaded vectors are copies: each one becomes a feature class in the project's
default geodatabase, inside a **GoBase** group layer.

## Build

Targets ArcGIS Pro **3.7** (.NET 10). References come from Esri's NuGet
package `Esri.ArcGISPro.Extensions30`, so ArcGIS Pro does not have to be
installed to compile.

- **Windows (to run it):** Visual Studio 2026 with the ArcGIS Pro SDK
  extension. Open `GoBase.slnx`, build, then start: Pro launches with the
  add-in loaded (`Properties/launchSettings.json`). The build also writes
  `GoBase.esriAddinX`, which installs on any machine by double-clicking it.
- **Linux (compile check only):** `dotnet build` with the .NET 10 SDK.
  `EnableWindowsTargeting` lets it compile the WPF project. Esri's packaging
  step does not run under `dotnet build`, so no `.esriAddinX` is produced.
  Nothing can be run here.

## Test against the server

1. On the backend, an admin creates an API key for you in Django admin.
2. In Pro: open a map, click **GoBase**, enter the server address
   (e.g. `https://portal.example`) and the key.
3. Catalog → pick a vector → **Zoom to** → **Add to map**.

The add-in calls the server over HTTPS. The host must be in the backend's
`DJANGO_ALLOWED_HOSTS`.
