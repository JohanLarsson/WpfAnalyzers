namespace WpfAnalyzers.Test.PropertyChanged.WPF1013UseCallerMemberNameTests
{
    using System.Threading.Tasks;
    using NUnit.Framework;
    using WpfAnalyzers.PropertyChanged;

    internal class CodeFix : CodeFixVerifier<WPF1013UseCallerMemberName, ImplementINotifyPropertyChangedCodeFixProvider>
    {
        [TestCase("null")]
        [TestCase("string.Empty")]
        [TestCase(@"""Bar""")]
        [TestCase(@"nameof(Bar)")]
        [TestCase(@"nameof(this.Bar)")]
        public async Task CallsOnPropertyChanged(string propertyName)
        {
            var testCode = @"
using System.ComponentModel;
using System.Runtime.CompilerServices;

public class ViewModel : INotifyPropertyChanged
{
    private int bar;

    public event PropertyChangedEventHandler PropertyChanged;

    public int Bar
    {
        get { return this.bar; }
        set
        {
            if (value == this.bar) return;
            this.bar = value;
            this.OnPropertyChanged(nameof(Bar));
        }
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}";

            testCode = testCode.AssertReplace(@"nameof(Bar)", propertyName);

            var expected = this.CSharpDiagnostic().WithLocationIndicated(ref testCode).WithMessage("Implement INotifyPropertyChanged.");
            await this.VerifyCSharpDiagnosticAsync(testCode, expected).ConfigureAwait(false);

            var fixedCode = @"using System.ComponentModel;
using System.Runtime.CompilerServices;

public class Foo : INotifyPropertyChanged
{
    private int value;

    public event PropertyChangedEventHandler PropertyChanged;

    public int Value
    {
        get
        {
            return this.value;
        }
        set
        {
            if (value == this.bar) return;
            this.bar = value;
            this.OnPropertyChanged(nameof(Bar));
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}";
            await this.VerifyCSharpFixAsync(testCode, fixedCode, allowNewCompilerDiagnostics: true)
                    .ConfigureAwait(false);
        }
    }
}