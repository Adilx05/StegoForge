using System.Reflection;
using StegoForge.Core.Abstractions;
using StegoForge.Core.Errors;
using StegoForge.Core.Models;
using Xunit;

namespace StegoForge.Tests.Unit;

public sealed class CoreContractFinalizationTests
{
    private static readonly IReadOnlyDictionary<Type, Func<StegoForgeException>> MapperCoverageFactories =
        new Dictionary<Type, Func<StegoForgeException>>
        {
            [typeof(UnsupportedFormatException)] = () => new UnsupportedFormatException("unsupported"),
            [typeof(WrongPasswordException)] = () => new WrongPasswordException("wrong password"),
            [typeof(InvalidPayloadException)] = () => new InvalidPayloadException("invalid payload"),
            [typeof(InvalidHeaderException)] = () => new InvalidHeaderException("invalid header"),
            [typeof(InsufficientCapacityException)] = () => new InsufficientCapacityException(128, 64),
            [typeof(OutputExistsException)] = () => new OutputExistsException("out.png"),
            [typeof(InternalProcessingException)] = () => new InternalProcessingException("internal failure")
        };

    private static readonly IReadOnlyCollection<StegoErrorCode> MapperCoverageCodes =
    [
        StegoErrorCode.UnsupportedFormat,
        StegoErrorCode.WrongPassword,
        StegoErrorCode.InvalidPayload,
        StegoErrorCode.InvalidHeader,
        StegoErrorCode.InsufficientCapacity,
        StegoErrorCode.OutputAlreadyExists,
        StegoErrorCode.InternalProcessingFailure
    ];

    [Fact]
    public void ServiceInterfaces_Exist_WithAsyncMethodsThatAcceptCancellationToken()
    {
        AssertAsyncServiceContract<IEmbedService>("EmbedAsync", typeof(EmbedRequest), typeof(EmbedResponse));
        AssertAsyncServiceContract<IExtractService>("ExtractAsync", typeof(ExtractRequest), typeof(ExtractResponse));
        AssertAsyncServiceContract<ICapacityService>("GetCapacityAsync", typeof(CapacityRequest), typeof(CapacityResponse));
        AssertAsyncServiceContract<IInfoService>("GetInfoAsync", typeof(InfoRequest), typeof(CarrierInfoResponse));
    }

    [Fact]
    public void RequestModels_RejectNullOrEmptyCriticalFields()
    {
        Assert.Throws<ArgumentException>(() => new EmbedRequest("carrier.png", "out.png", []));
        Assert.Throws<ArgumentException>(() => new EmbedRequest(" ", "out.png", [1]));
        Assert.Throws<ArgumentException>(() => new EmbedRequest("carrier.png", null!, [1]));

        Assert.Throws<ArgumentException>(() => new ExtractRequest("carrier.png", " "));
        Assert.Throws<ArgumentException>(() => new ExtractRequest(null!, "out.bin"));

        Assert.Throws<ArgumentException>(() => new CapacityRequest("", 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CapacityRequest("carrier.png", -1));

        Assert.Throws<ArgumentException>(() => new InfoRequest("\t"));
    }

    [Fact]
    public void ResponseModels_AlwaysExposeDiagnosticsContainers()
    {
        var embed = new EmbedResponse("out.png", "png-lsb-v1", 8, 16);
        var extract = new ExtractResponse("out.bin", "./out.bin", "png-lsb-v1", [1, 2], false, false);
        var capacity = new CapacityResponse("png-lsb-v1", 64, 1024, 2048, 1536, 128, true, 960);
        var info = new CarrierInfoResponse(
            "png-lsb-v1",
            new CarrierFormatDetails("png-lsb-v1", "PNG LSB", "1.0.0"),
            4096,
            2048,
            1536,
            embeddedDataPresent: false,
            supportsEncryption: true,
            supportsCompression: true);

        AssertSameDiagnosticsContract(embed.Diagnostics);
        AssertSameDiagnosticsContract(extract.Diagnostics);
        AssertSameDiagnosticsContract(capacity.Diagnostics);
        AssertSameDiagnosticsContract(info.Diagnostics);
    }

    [Fact]
    public void ErrorMapper_HandlesEveryDeclaredStegoForgeExceptionSubtype()
    {
        var declaredSubtypes = typeof(StegoForgeException)
            .Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(StegoForgeException).IsAssignableFrom(type))
            .ToHashSet();

        Assert.Equal(declaredSubtypes, MapperCoverageFactories.Keys.ToHashSet());

        foreach (var (exceptionType, factory) in MapperCoverageFactories)
        {
            var exception = factory();
            var mapped = StegoErrorMapper.FromException(exception);

            Assert.Equal(exception.Code, mapped.Code);
            Assert.False(string.IsNullOrWhiteSpace(mapped.Message));
            Assert.Equal(exceptionType, exception.GetType());
        }
    }

    [Fact]
    public void ErrorMapperCoverageData_MustMatchDeclaredStegoErrorCodes()
    {
        var declaredCodes = Enum.GetValues<StegoErrorCode>().ToHashSet();
        var coverageCodes = MapperCoverageCodes.ToHashSet();

        Assert.Equal(declaredCodes, coverageCodes);

        foreach (var exception in MapperCoverageFactories.Values.Select(factory => factory()))
        {
            Assert.Contains(exception.Code, MapperCoverageCodes);
        }
    }

    [Fact]
    public void ContractSnapshot_KeyDtoPropertyNames_AreStableForSerializationBoundaries()
    {
        var expectedPropertyNames = new Dictionary<Type, string[]>
        {
            [typeof(EmbedRequest)] = ["CarrierPath", "OutputPath", "Payload", "ProcessingOptions", "PasswordOptions"],
            [typeof(EmbedResponse)] = ["OutputPath", "CarrierFormatId", "PayloadSizeBytes", "BytesEmbedded", "Diagnostics"],
            [typeof(ExtractRequest)] = ["CarrierPath", "OutputPath", "ProcessingOptions", "PasswordOptions"],
            [typeof(ExtractResponse)] =
            [
                "OutputPath", "ResolvedOutputPath", "CarrierFormatId", "Payload", "PayloadSizeBytes",
                "OriginalFileName", "PreservedOriginalFileName", "IntegrityVerificationResult", "Warnings",
                "WasCompressed", "WasEncrypted", "Diagnostics"
            ],
            [typeof(CapacityRequest)] = ["CarrierPath", "PayloadSizeBytes", "ProcessingOptions"],
            [typeof(CapacityResponse)] =
            [
                "CarrierFormatId", "RequestedPayloadSizeBytes", "AvailableCapacityBytes", "MaximumCapacityBytes",
                "SafeUsableCapacityBytes", "EstimatedOverheadBytes", "CanEmbed", "RemainingBytes", "FailureReason",
                "ConstraintBreakdown", "Diagnostics"
            ],
            [typeof(InfoRequest)] = ["CarrierPath", "ProcessingOptions"],
            [typeof(CarrierInfoResponse)] =
            [
                "FormatId", "FormatDetails", "CarrierSizeBytes", "EstimatedCapacityBytes", "AvailableCapacityBytes",
                "EmbeddedDataPresent", "SupportsEncryption", "SupportsCompression", "PayloadMetadata",
                "ProtectionDescriptors", "Diagnostics"
            ]
        };

        foreach (var (type, expected) in expectedPropertyNames)
        {
            var actual = type
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Select(property => property.Name)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();

            Assert.Equal(expected.OrderBy(name => name, StringComparer.Ordinal).ToArray(), actual);
        }
    }

    private static void AssertAsyncServiceContract<TService>(string methodName, Type requestType, Type responseType)
    {
        var serviceType = typeof(TService);
        Assert.True(serviceType.IsInterface);

        var method = serviceType.GetMethod(methodName);
        Assert.NotNull(method);

        var parameters = method!.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(requestType, parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);

        Assert.Equal(typeof(Task<>).MakeGenericType(responseType), method.ReturnType);
    }

    private static void AssertSameDiagnosticsContract(OperationDiagnostics diagnostics)
    {
        Assert.NotNull(diagnostics);
        Assert.NotNull(diagnostics.Warnings);
        Assert.NotNull(diagnostics.Notes);
    }
}
