# Archivist.

![CI](https://github.com/vuysanan/Archivist/actions/workflows/ci.yml/badge.svg)

A motion-driven metadata archive for music. Search a track or an album and instantly see its production credits (lead producer, co-producers, writers, mixing engineer) and an interactive **Sample Tree** of the songs it samples or interpolates.

**Live demo:** https://music-archivist.vercel.app/

<!-- Add a screenshot: save it as docs/screenshot.png and uncomment the line below -->
<!-- ![Archivist screenshot](docs/screenshot.png) -->

## Features

- **Track search:** credits, release year and cover art.
- **Album search:** cover, year and a numbered tracklist. Selecting a track opens its credits.
- **Sample Tree:** select any sample to search for the original song and follow the chain back.
- **30-second previews:** play any sample straight from the Sample Tree.
- **Community suggestions:** visitors can suggest a missing sample. Every suggestion is reviewed before it appears, and approved ones carry a "Community verified" badge.
- **Friendly error screens** for not found, server problems, offline and too many requests.
- A dark, minimal interface with spring-based staggered entrance animations and animated screen transitions.

## Tech stack

| Part | Technology |
| --- | --- |
| Frontend | React, Vite, Tailwind CSS, Framer Motion, lucide-react |
| Backend | ASP.NET Core 8 Web API (C#) |
| Database | PostgreSQL (Neon) with Entity Framework Core, for community suggestions |
| Data sources | [Deezer API](https://developers.deezer.com/api) (basics, previews) and [Genius API](https://docs.genius.com/) (credits and samples) |
| Testing and CI | xUnit, GitHub Actions |
| Hosting | Vercel (frontend), Render in a Docker container (backend), Neon (database) |

## How it works

```
Browser (React on Vercel)
        |  search, album, preview and suggestion requests
        v
ASP.NET Core API (Render)
        |-- Deezer: title, artist, cover art, year, tracklist, previews
        |-- Genius: producers, writers, mixing engineer, samples
        |-- PostgreSQL (Neon): community sample suggestions
        |-- Discord webhook: owner notification for new suggestions
        '-- In-memory cache (12 h) and per-visitor rate limiting
```

**Matching.** Deezer and Genius share no common ID, so the backend matches them by comparing cleaned-up song titles and artist names. Genius results are only used when both the title and the artist match, so credits for a different song are never shown. If no match is found, the credits show "Not listed". Deezer results are ranked so that the original release is preferred over karaoke, tribute and re-upload versions.

**Resilience.** If Genius is unavailable, the page still shows the Deezer details. Search results are cached for 12 hours, and each visitor is rate limited to protect the usage limits of the public APIs.

## Community suggestions

Anyone can suggest a missing sample from a track page, with no account needed. Suggestions are stored as **pending** and stay hidden until an admin approves them. Spam is limited by a hidden trap field, duplicate detection and a per-visitor limit on how many suggestions can be sent per hour. The review page is protected by a secret admin key. The site owner is notified of each new suggestion through a private Discord channel.

## Testing

The backend has unit and integration tests (xUnit), and GitHub Actions runs them, plus a frontend build, on every push.

- **Unit tests** cover the matching logic: name cleaning, choosing the right Deezer result, and merging community samples. Several replay real wrong-result bugs found during testing, so they can't quietly come back.
- **Integration tests** start the API in memory with fake music services and check the endpoints, error responses, rate limiting, CORS, and the suggestion and review flow.

## Deployment

The React frontend is deployed on Vercel. The API runs in a Docker container on Render, in an EU region so that Deezer returns its full catalogue. Community suggestions are stored in a Neon PostgreSQL database. Secrets such as the Genius token and the admin key are kept in the hosting services' environment settings and are never committed to the repository.

## Project structure

```
Archivist/
  backend/
    Archivist.Api/         ASP.NET Core API (controllers, services, data, models, Dockerfile)
    Archivist.Api.Tests/   unit and integration tests
  frontend/                React + Vite app
  .github/workflows/       CI pipeline
```

## Known limitations

- The free Render plan sleeps after 15 minutes without traffic, so the first search after a break can take a while.
- Genius credits and samples are crowd-sourced, so some songs have gaps.
- Search works best with the artist included, for example `Kendrick Lamar DNA`.
- Deezer and Genius have their own usage limits, which is why results are cached and requests are rate limited.
- Song previews depend on Deezer, and not every song has one.

## Credits

Music metadata from Deezer and Genius. This project isn't affiliated with either service.