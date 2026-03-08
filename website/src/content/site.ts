export interface FeatureItem {
  title: string;
  description: string;
}

export interface CliExample {
  command: string;
  description: string;
}

export interface RoadmapPhase {
  title: string;
  summary: string;
}

export const siteContent = {
  hero: {
    eyebrow: 'StegoForge',
    headline: 'StegoForge is a modular .NET steganography toolkit for reliable carrier workflows.',
    subheadline:
      'Use the deterministic CLI for automation or the WPF desktop app for guided operations across BMP, PNG, and WAV carriers.',
    primaryCta: {
      label: 'Repository',
      href: 'https://github.com/Adilx05/StegoForge'
    },
    secondaryCta: {
      label: 'Releases',
      href: 'https://github.com/Adilx05/StegoForge/releases'
    }
  },
  overviewLead:
    'StegoForge is a layered steganography platform with shared orchestration services and delivery surfaces for both scripted and desktop usage.',
  featuresLead:
    'Current implementation emphasizes deterministic behavior, policy validation, and clear boundaries between UI and carrier handlers.',
  features: [
    {
      title: 'Layered architecture',
      description: 'Core contracts, application orchestration, and provider modules keep UI concerns separate from carrier logic.'
    },
    {
      title: 'Deterministic CLI contracts',
      description: 'Stable exit codes and JSON error shapes are designed for scripting and CI automation.'
    },
    {
      title: 'Policy-aware workflows',
      description: 'Embed and extract requests enforce compression/encryption policy validation before handler I/O.'
    },
    {
      title: 'Test-first reliability',
      description: 'Unit, integration, CLI, and WPF suites validate round trips and failure semantics.'
    }
  ] as FeatureItem[],
  cliLead: 'Realistic commands from the repository root for common automation and diagnostics workflows.',
  cliExamples: [
    {
      command:
        'dotnet run --project src/StegoForge.Cli -- embed --carrier samples/input.png --payload samples/message.bin --out artifacts/output.png --compress auto --encrypt aes-gcm --password-env STEGOFORGE_PASSWORD',
      description: 'Embeds a payload into a PNG carrier with automatic compression and AES-GCM encryption from an environment-provided password.'
    },
    {
      command:
        'dotnet run --project src/StegoForge.Cli -- extract --carrier artifacts/output.png --out artifacts/recovered.bin --password-env STEGOFORGE_PASSWORD',
      description: 'Extracts and decrypts the payload from the stego carrier back to a binary file.'
    },
    {
      command:
        'dotnet run --project src/StegoForge.Cli -- capacity --carrier samples/input.wav --payload 65536 --compress auto --json',
      description: 'Checks whether a WAV carrier can hold a 64 KiB payload and emits a machine-readable capacity report.'
    },
    {
      command: 'dotnet run --project src/StegoForge.Cli -- info --carrier artifacts/output.png --json',
      description: 'Inspects carrier metadata and embedded envelope details for troubleshooting and pipeline checks.'
    }
  ] as CliExample[],
  wpf: {
    lead: 'StegoForge.Wpf provides a Windows-native workflow for interactive embed/extract operations.',
    useCases: [
      'Interactive validation before running long operations.',
      'Guided parameter entry for encryption/compression combinations.',
      'Operator-friendly status and diagnostics when triaging failed extracts.'
    ],
    launchCommand: 'dotnet run --project src/StegoForge.Wpf',
    buildCommand: 'dotnet build src/StegoForge.Wpf/StegoForge.Wpf.csproj',
    docsHref: 'docs/gui.md'
  },
  installation: {
    lead: 'Quickstart for CLI and test validation using the pinned .NET SDK (10.0.100).',
    commands: [
      'git clone https://github.com/Adilx05/StegoForge.git',
      'cd StegoForge',
      'dotnet restore',
      'dotnet build',
      'dotnet test tests/StegoForge.Tests.Unit/StegoForge.Tests.Unit.csproj',
      'dotnet run --project src/StegoForge.Cli -- help'
    ],
    docsHref: 'docs/building.md'
  },
  ctas: {
    lead: 'Get binaries from tagged releases or review source, docs, and issues in the repository.',
    releases: {
      label: 'GitHub Releases',
      href: 'https://github.com/Adilx05/StegoForge/releases'
    },
    repository: {
      label: 'GitHub Repository',
      href: 'https://github.com/Adilx05/StegoForge'
    }
  },
  roadmapLead: 'Roadmap summary synced with docs/roadmap.md milestones 1-14.',
  roadmap: [
    {
      title: 'Milestones 1-2: Foundation and contracts',
      summary: 'Solution scaffolding and stable core abstractions for embed/extract/capacity/info contracts.'
    },
    {
      title: 'Milestones 3-5: Payload, compression, and crypto',
      summary: 'Versioned payload envelope, pluggable compression, and authenticated encryption with KDF policy support.'
    },
    {
      title: 'Milestones 6-8: Carrier format coverage',
      summary: 'Production PNG/BMP/WAV LSB handlers with deterministic capacity and validation behavior.'
    },
    {
      title: 'Milestones 9-11: Delivery surfaces',
      summary: 'Application orchestration hardening plus stable CLI command surface and first usable WPF GUI.'
    },
    {
      title: 'Milestones 12-14: Hardening to release',
      summary: 'Robustness, documentation/developer-experience completion, and v1.0 release readiness.'
    }
  ] as RoadmapPhase[]
};
