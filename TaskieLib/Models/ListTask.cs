using System.Collections.ObjectModel;
using System.ComponentModel;
using System;

public class ListTask : INotifyPropertyChanged
{
    private DateTime _creationDate;
    private DateTime? _parentCreationDate;
    private DateTime? _reminderDateTime;
    private string _name;
    private bool _isDone;
    private ObservableCollection<ListTask> _subTasks;

    public ListTask()
    {
        _subTasks = new ObservableCollection<ListTask>();
    }

    public DateTime? ReminderDateTime
    {
        get
        {
            if (_reminderDateTime < DateTime.Now) {
                this.ReminderDateTime = null;
                return null;
            }
            else
            {
                return _reminderDateTime;
            }
        }
        set
        {
            if (_reminderDateTime != value)
            {
                _reminderDateTime = value;
                OnPropertyChanged(nameof(ReminderDateTime));
            }
        }
    }

    public ObservableCollection<ListTask> SubTasks
    {
        get { return _subTasks; }
        set
        {
            if (_subTasks != value)
            {
                _subTasks = value;
                OnPropertyChanged(nameof(SubTasks));
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

    public DateTime? ParentCreationDate
    {
        get { return _parentCreationDate; }
        set
        {
            if (_parentCreationDate != value)
            {
                _parentCreationDate = value;
                OnPropertyChanged(nameof(ParentCreationDate));
            };
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
