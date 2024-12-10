using System.Collections.Generic;
using System;
using System.ComponentModel;
using System.Collections.ObjectModel;

// Object of a task inside a list
public class ListTask : INotifyPropertyChanged
{
    private DateTime _creationDate;
    private string _name;
    private bool _isDone;
    private ObservableCollection<ListTask> _subTasks = new ObservableCollection<ListTask>();


    public ObservableCollection<ListTask> SubTasks
    {
        get { return _subTasks; }
        set
        {
            if (_subTasks != value) {
                _subTasks = value;
                OnPropertyChanged(nameof(CreationDate));
            }
        }
    }
    public DateTime CreationDate
    {
        get { return _creationDate; }
        set
        {
            if (_creationDate != value)
            {
                _creationDate = value;
                OnPropertyChanged(nameof(CreationDate));
            }
        }
    }

    public string Name
    {
        get { return _name; }
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }
    }

    public bool IsDone
    {
        get { return _isDone; }
        set
        {
            if (_isDone != value)
            {
                _isDone = value;
                OnPropertyChanged(nameof(IsDone));
            }
        }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
