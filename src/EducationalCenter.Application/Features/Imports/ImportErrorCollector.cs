namespace EducationalCenter.Application.Features.Imports;

internal sealed class ImportErrorCollector
{
    private const int MaxStored = 500;
    private readonly List<ImportError> _errors = [];

    public int Count { get; private set; }
    public IReadOnlyList<ImportError> Errors => _errors;

    public void Add(int rowNumber, string column, string message)
    {
        Count++;
        if (_errors.Count < MaxStored)
            _errors.Add(new ImportError(rowNumber, column, message));
    }
}
