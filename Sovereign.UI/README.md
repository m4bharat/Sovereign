# Sovereign Step 20 Angular MVP — Production Structured

Angular frontend for the Sovereign MVP with separated files, feature-based architecture, and local API integration.

## Features
- Rewrite
- Relationships
- Decay Radar
- Assistant

## Architecture
- `core/` shared infrastructure, services, models, interceptors
- `features/` feature-specific pages and facades
- `shared/` reusable layout and UI components

## Run
```bash
npm install
npm start
```

## Build
```bash
npm run build
```

## Configure API
Update `src/environments/environment.ts` if your local API uses a different port.
