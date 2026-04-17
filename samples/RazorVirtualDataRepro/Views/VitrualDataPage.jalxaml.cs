using Jalium.UI;
using Jalium.UI.Collections;
using Jalium.UI.Controls;

namespace RazorVirtualDataRepro.Views;

/// <summary>
/// User-provided reproduction page for @if(#.Adult) inside a DataTemplate.
/// 1000 Person rows. Adult = (i % 3) >= 1 — every third row should hide Age.
/// </summary>
public partial class VitrualDataPage : Page
{
    public VitrualDataPage()
    {
        InitializeComponent();

        DataContext = new DataSource
        {
            Persons = new ObservableCollection<Person>(
                Enumerable.Range(1, 1000).Select(i => new Person
                {
                    Name = $"Person {i}",
                    Age = 20 + (i % 30),
                    Adult = (i % 3) >= 1,
                })),
        };
    }

    public class DataSource
    {
        public ObservableCollection<Person> Persons { get; set; } = new();
    }

    public class Person : ViewModelBase
    {
        private string _name = string.Empty;
        private int _age;
        private bool _adult;

        public string Name { get => _name; set => SetProperty(ref _name, value); }
        public int Age { get => _age; set => SetProperty(ref _age, value); }
        public bool Adult { get => _adult; set => SetProperty(ref _adult, value); }
    }
}
