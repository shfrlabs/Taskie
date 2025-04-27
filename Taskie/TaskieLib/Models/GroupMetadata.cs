using System;
using System.ComponentModel;
using System.Drawing;

public class GroupMetadata : INotifyPropertyChanged {
    private DateTime _creationDate;
    private string? _name;
    private Color _color;
    private int? _ID;

    public DateTime CreationDate {
        get => _creationDate;
        set {
            if (_creationDate != value) {
                _creationDate = value;
                OnPropertyChanged(nameof(CreationDate));
            }
        }
    }

    public string? Name {
        get => _name;
        set {
            if (_name != value) {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    public Color GColor {
        get => _color;
        set {
            if (_color != value) {
                _color = value;
                OnPropertyChanged(nameof(GColor));
            }
        }
    }

    public int? ID {
        get => _ID;
        set {
            if (value != _ID) {
                _ID = value;
                OnPropertyChanged(nameof(ID));
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName) {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
