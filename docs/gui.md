# GUI (WPF)

The WPF app (`src/StegoForge.Wpf`) is the desktop user interface for StegoForge.

## Finalized v1 UX behavior

### Embed form

Fields and controls in `EmbedView`/`EmbedViewModel`:

- **Carrier file** (`CarrierPath`) with Browse + drag/drop support.
- **Payload file** (`PayloadPath`) with Browse + drag/drop support.
- **Output file** (`OutputPath`) with Browse + drag/drop support.
- **Password** (`Password`) text input.
- **Require encryption** (`RequireEncryption`) checkbox.
- **Allow overwrite existing output** (`AllowOverwrite`) checkbox.
- Actions: **Check capacity**, **Get carrier info**, **Embed**.
- Read-only operation output pane bound to `ResultMessage`.

Implementation references:

- `src/StegoForge.Wpf/Views/EmbedView.xaml`
- `src/StegoForge.Wpf/ViewModels/EmbedViewModel.cs`

### Extract form

Fields and controls in `ExtractView`/`ExtractViewModel`:

- **Carrier file** (`CarrierPath`) with Browse + drag/drop support.
- **Output folder/file** (`OutputPath`) with Browse + drag/drop support.
- **Password** (`Password`) text input.
- **Require encryption** (`RequireEncryption`) checkbox.
- **Allow overwrite existing output** (`AllowOverwrite`) checkbox.
- Action: **Extract**.
- Read-only operation output pane bound to `ResultMessage`.

Implementation references:

- `src/StegoForge.Wpf/Views/ExtractView.xaml`
- `src/StegoForge.Wpf/ViewModels/ExtractViewModel.cs`

## Validation behavior (v1)

Validation is performed in view models through `UiOperationPolicyValidator` and surfaced via `INotifyDataErrorInfo` bindings.

- Validation runs on each relevant property change.
- Field-level validation messages are shown beneath each textbox via WPF validation error binding.
- Command availability is validation-aware:
  - Embed actions are disabled when required fields/options are invalid.
  - Extract action is disabled when required fields/options are invalid.
- Drag/drop path application rejects invalid paths and shows a deterministic "Invalid file path" notification.
- Validation rules align with shared application policy semantics (including encryption/password policy and overwrite policy) to keep GUI/CLI behavior consistent.

Key references:

- `src/StegoForge.Wpf/Validation/UiOperationPolicyValidator.cs`
- `src/StegoForge.Wpf/ViewModels/EmbedViewModel.cs`
- `src/StegoForge.Wpf/ViewModels/ExtractViewModel.cs`
- `tests/StegoForge.Tests.Wpf/ViewModelValidationTests.cs`
- `tests/StegoForge.Tests.Wpf/ViewModelBrowseCommandTests.cs`

## Progress and error message semantics (v1)

All operation view models inherit shared operation-state fields from `OperationViewModelBase`.

### Shared state contract

- `ProgressText` defaults to `Idle`.
- `StatusMessage` defaults to `Ready.`
- `LastErrorCode`/`LastErrorMessage` are cleared at operation start.

### Embed semantics

- On user cancellation at confirmation prompt:
  - `StatusMessage = "Embed cancelled."`
  - `ProgressText = "Cancelled"`
  - `ResultMessage = "Embed cancelled by user."`
- During execution:
  - `ProgressText` transitions through `Preparing payload` then `Submitting embed request`.
- On success:
  - `StatusMessage = "Embed completed."`
  - `ProgressText = "Completed"`
  - `ResultMessage` includes bytes embedded, resolved output path, and carrier format id.
- On failure:
  - Exception is mapped through `StegoErrorMapper`.
  - `StatusMessage = "Embed failed."`
  - `ProgressText = "Failed"`
  - `ResultMessage` uses deterministic shape: `Embed failed (<Code>): <Message>`.
  - `LastErrorCode`/`LastErrorMessage` are populated and notification dialog is shown.

### Extract semantics

- On user cancellation at confirmation prompt:
  - `StatusMessage = "Extract cancelled."`
  - `ProgressText = "Cancelled"`
  - `ResultMessage = "Extract cancelled by user."`
- During execution:
  - `ProgressText` transitions through `Preparing request` then `Running extraction`.
- On success:
  - `StatusMessage = "Extract completed."`
  - `ProgressText = "Completed"`
  - `ResultMessage` includes extracted payload size, resolved output path, and carrier format id.
- On failure:
  - Exception is mapped through `StegoErrorMapper`.
  - `StatusMessage = "Extract failed."`
  - `ProgressText = "Failed"`
  - `ResultMessage` uses deterministic shape: `Extract failed (<Code>): <Message>`.
  - `LastErrorCode`/`LastErrorMessage` are populated and notification dialog is shown.

Behavior coverage:

- `tests/StegoForge.Tests.Wpf/ViewModelOperationStateTests.cs`
- `tests/StegoForge.Tests.Wpf/WpfCommandFlowTests.cs`

## MVVM and shared-service alignment

- Views declare bindings and input events only.
- View models own command orchestration, validation triggering, and UI state transitions.
- Application service calls (`IEmbedService`, `IExtractService`, `ICapacityService`, `IInfoService`) remain the source of domain behavior.
- Error mapping uses shared domain error codes so GUI messages stay consistent with CLI outcomes.
