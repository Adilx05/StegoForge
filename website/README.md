# StegoForge Website

This directory contains the Astro static site used for the StegoForge project website.

## Prerequisites

- **Node.js 20.x** (recommended; CI/GitHub Pages workflow uses Node 20).
- npm (bundled with Node.js).

From the repository root:

```bash
cd website
```

## Local development

Install dependencies and run the development server:

```bash
npm install
npm run dev
```

Astro will print a local URL (typically `http://localhost:4321`) for live preview.

## Production build and preview

Build the static site and preview the built output:

```bash
npm run build
npm run preview
```

Build output is written to `website/dist`.

## Base path behavior (`base`) for GitHub Pages vs custom domain

Astro base-path behavior is configured in [`astro.config.mjs`](./astro.config.mjs):

- `SITE_URL` controls the canonical site origin (`site`).
- `BASE_PATH` controls path prefixing (`base`) for generated asset and page URLs.

Current defaults:

- `SITE_URL` default: `https://example.github.io`
- `BASE_PATH` default: `/`

### GitHub Pages (project site)

For project pages (for example `https://<owner>.github.io/<repo>/`), `base` **must** be the repository path (`/<repo>/`).

The Pages workflow already handles this automatically in [`.github/workflows/pages.yml`](../.github/workflows/pages.yml):

- `actions/configure-pages` exposes `base_path`.
- Build step passes `BASE_PATH: ${{ steps.pages.outputs.base_path }}`.

### Custom domain

For a custom domain (for example `https://stegoforge.dev/`), use root base:

- `BASE_PATH=/`

Example local production-like build commands:

```bash
# GitHub Pages-style repo path
SITE_URL=https://<owner>.github.io BASE_PATH=/<repo>/ npm run build

# Custom domain root path
SITE_URL=https://stegoforge.dev BASE_PATH=/ npm run build
```

## Updating core content sections

Most homepage content is centralized in [`src/content/site.ts`](./src/content/site.ts).
Update these sections directly in that file:

- **Hero**: `siteContent.hero`
- **Features**: `siteContent.featuresLead` and `siteContent.features`
- **Roadmap**: `siteContent.roadmapLead` and `siteContent.roadmap`
- **Primary links / CTAs**:
  - hero buttons: `siteContent.hero.primaryCta` / `siteContent.hero.secondaryCta`
  - footer/action links: `siteContent.ctas`

After edits, run:

```bash
npm run build
```

## Troubleshooting (GitHub Pages path issues)

### Symptom: broken CSS/JS/assets after deploy

Typical cause: `base` mismatch (built for `/` but served from `/<repo>/`, or vice versa).

Checklist:

1. Confirm the expected hosting path:
   - Project Pages: `https://<owner>.github.io/<repo>/` → `BASE_PATH=/<repo>/`
   - Custom domain root: `https://<domain>/` → `BASE_PATH=/`
2. Verify build-time env values used by CI/local build (`SITE_URL`, `BASE_PATH`).
3. Rebuild and redeploy with correct `BASE_PATH`.
4. Hard refresh browser / clear cache if stale assets are still referenced.

### Symptom: links work locally but fail on Pages

Local `npm run dev` often runs at `/`, which can hide base-path mistakes.
Validate using a production build with explicit env vars:

```bash
SITE_URL=https://<owner>.github.io BASE_PATH=/<repo>/ npm run build
npm run preview
```

If links/assets work in preview with the same `BASE_PATH`, deploy output should also resolve correctly.
