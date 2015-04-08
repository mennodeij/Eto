using System.Collections.Generic;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Eto
{
	/// <summary>
	/// A storage for properties and events of a class
	/// </summary>
	/// <remarks>
	/// This is used by <see cref="Widget"/> object to minimize the footprint of each instance.
	/// For example, the <see cref="Forms.Control"/> class has around 20 events, each would take up to 4 bytes on a 32 bit 
	/// system for a total overhead of 80 bytes per instance.
	/// Most of the events won't be handled on most controls, so using a dictionary can dramatically reduce the size.
	/// 
	/// This can also be used for rarely used properties that do not need to be extremely performant when getting or setting the value.
	/// </remarks>
	public class PropertyStore : Dictionary<object, object>
	{
		/// <summary>
		/// Gets the parent widget that this property store is attached to
		/// </summary>
		/// <remarks>
		/// This is used to attach/remove events
		/// </remarks>
		/// <value>The parent widget</value>
		public Widget Parent { get; private set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Eto.PropertyStore"/> class.
		/// </summary>
		/// <param name="parent">Parent to attach the properties to</param>
		internal PropertyStore(Widget parent)
		{
			this.Parent = parent;
		}

		/// <summary>
		/// Gets a value from the property store with the specified key of a concrete type
		/// </summary>
		/// <param name="key">Key of the property to get</param>
		/// <typeparam name="T">The type of property to get.</typeparam>
		/// <returns>Value of the property with the given key, or default(T) if not found</returns>
		public T Get<T>(object key)
		{
			object value;
			return TryGetValue(key, out value) ? (T)value : default(T);
		}

		/// <summary>
		/// Gets a value from the property store with the specified key of a concrete type, and creates a new instance if it doesn't exist yet.
		/// </summary>
		/// <param name="key">Key of the property to get</param>
		/// <typeparam name="T">Type type of property to get.</typeparam>
		/// <returns>Value of the property with the given key, or a new instance if not already added</returns>
		public T Create<T>(object key)
			where T: new()
		{
			return Create<T>(key, () => new T());
		}

		/// <summary>
		/// Gets a value from the property store with the specified key of a concrete type, and creates a new instance if it doesn't exist yet.
		/// </summary>
		/// <param name="key">Key of the property to get</param>
		/// <param name="create">Delegate to create the object, if it doesn't already exist</param>
		/// <typeparam name="T">Type type of property to get.</typeparam>
		/// <returns>Value of the property with the given key, or a new instance if not already added</returns>
		public T Create<T>(object key, Func<T> create)
		{
			object value;
			if (!TryGetValue(key, out value))
			{
				value = create();
				Add(key, value);
			}
			return (T)value;
		}

		/// <summary>
		/// Adds a generic event delegate with the specified key
		/// </summary>
		/// <remarks>
		/// This should be called in an event's add accessor.
		/// If you are adding a handler-based event, call <see cref="AddHandlerEvent"/> instead, which will automatically
		/// tell the handler that it needs to be wired up.
		/// 
		/// You can use any subclass of <see cref="System.EventArgs"/> for the type of event handler
		/// 
		/// To trigger the event, use <see cref="TriggerEvent{T}"/>.
		/// </remarks>
		/// <seealso cref="RemoveEvent"/>
		/// <seealso cref="AddHandlerEvent"/>
		/// <example>
		/// Example implementation of a generic event
		/// <code>
		/// 	static readonly object MySomethingEventKey = new object();
		/// 	
		/// 	public event EventHandler&lt;EventArgs&gt; MySomething
		/// 	{
		/// 		add { Properties.AddEvent(MySomethingEvent, value); }
		/// 		remove { Properties.RemoveEvent(MySomethingEvent, value); }
		/// 	}
		/// </code>
		/// </example>
		/// <param name="key">Key of the event to add to</param>
		/// <param name="value">Delegate to add to the event</param>
		public void AddEvent(object key, Delegate value)
		{
			object existingDelegate;
			if (TryGetValue(key, out existingDelegate))
				this[key] = Delegate.Combine((Delegate)existingDelegate, value);
			else
			{
				Add(key, value);
			}
		}

		/// <summary>
		/// Adds a handler-based event delegate with the specified key
		/// </summary>
		/// <remarks>
		/// This should be called in an event's add accessor.
		/// This is used for any event that should be triggered by the platform handler. 
		/// This will call <see cref="M:Eto.Widget.IHandler.HandleEvent(string,bool)"/> with the specified <paramref name="key"/> for the
		/// first subscription to the event.
		/// 
		/// You can use any subclass of <see cref="System.EventArgs"/> for the type of event handler
		/// 
		/// To trigger the event, use <see cref="TriggerEvent{T}"/>
		/// </remarks>
		/// <example>
		/// Example implementation of a handler-triggered event
		/// <code>
		/// 	public const string MySomethingEvent = "MyControl.MySomething";
		/// 	
		/// 	public event EventHandler&lt;EventArgs&gt; MySomething
		/// 	{
		/// 		add { Properties.AddHandlerEvent(MySomethingEvent, value); }
		/// 		remove { Properties.RemoveHandlerEvent(MySomethingEvent, value); }
		/// 	}
		/// </code>
		/// </example>
		/// <param name="key">Key of the event to add to</param>
		/// <param name="value">Delegate to add to the event</param>
		public void AddHandlerEvent(string key, Delegate value)
		{
			object existingDelegate;
			if (TryGetValue(key, out existingDelegate))
				this[key] = Delegate.Combine((Delegate)existingDelegate, value);
			else
			{
				if (!EventLookup.IsDefault(Parent, key))
				{
					var handler = Parent.Handler as Widget.IHandler;
					if (handler != null)
					{
						handler.HandleEvent(key);
					}
				}
				Add(key, value);
			}
		}

		/// <summary>
		/// Removes the event delegate with the specified <paramref name="key"/>
		/// </summary>
		/// <remarks>
		/// Use this in the remove accessor of your event.  See <see cref="AddEvent"/> and <see cref="AddHandlerEvent"/>
		/// for examples.
		/// </remarks>
		/// <param name="key">Key of the event to remove</param>
		/// <param name="value">Delegate to remove from the event</param>
		public void RemoveEvent(object key, Delegate value)
		{
			object existingDelegate;
			if (TryGetValue(key, out existingDelegate))
			{
				this[key] = Delegate.Remove((Delegate)existingDelegate, value);
			}
		}

		/// <summary>
		/// Triggers an event with the specified key
		/// </summary>
		/// <remarks>
		/// Call this in your OnMyEvent(EventArgs) method to trigger the event if it has been subscribed to.
		/// This can handle events that have any type of EventArgs.
		/// </remarks>
		/// <example>
		/// This shows how to trigger either a generic event or handler-triggered event:
		/// <code>
		/// 	protected virtual void OnMySomething(EventArgs e)
		/// 	{
		/// 		Properties.TriggerEvent(MySomethingEventKey, this, e);
		/// 	}
		/// </code>
		/// </example>
		/// <param name="key">Key of the generic or handler event</param>
		/// <param name="sender">Object sending the event (usually 'this')</param>
		/// <param name="args">Arguments for the event</param>
		/// <typeparam name="T">Type of the event arguments</typeparam>
		public void TriggerEvent<T>(object key, object sender, T args)
			where T: EventArgs
		{
			object existingDelegate;
			if (TryGetValue(key, out existingDelegate) && existingDelegate != null)
			{
				((EventHandler<T>)existingDelegate)(sender, args);
			}
		}

		/// <summary>
		/// Set the value for the specified property key, removing the value from the dictionary if it is the default value of T.
		/// </summary>
		/// <remarks>
		/// This can be used as an optimized way to set the value in the dictionary as if the value set is the default
		/// (e.g. null for reference types, false for bool, 0 for int, etc), then it will be removed from the dictionary
		/// instead of just set to the value, reducing memory usage.
		/// The <see cref="Get{T}"/> will return the default value if it does not exist
		/// </remarks>
		/// <param name="key">Key of the property to set.</param>
		/// <param name="value">Value for the property.</param>
		/// <typeparam name="T">The type of the property to set.</typeparam>
		public void Set<T>(object key, T value)
		{
			if (ContainsKey(key) && Equals(value, default(T)))
				Remove(key);
			else
				this[key] = value;
		}

		/// <summary>
		/// Set the value for the specified property key, raising the <paramref name="propertyChanged"/> handler if it has changed.
		/// </summary>
		/// <remarks>
		/// This is useful when creating properties that need to trigger changed events without having to write boilerplate code.
		/// </remarks>
		/// <example>
		/// <code>
		/// public class MyForm : Form, INotifyPropertyChanged
		/// {
		/// 	static readonly MyPropertyKey = new object();
		/// 
		/// 	public bool MyProperty
		///		{
		/// 		get { return Properties.Get&lt;bool&gt;(MyPropertyKey); }
		/// 		set { Properties.Set(MyPropertyKey, value, PropertyChanged); }
		/// 	}
		/// 
		/// 	public event PropertyChangedEventHandler PropertyChanged;
		/// }
		/// </code>
		/// </example>
		/// <param name="key">Key of the property to set.</param>
		/// <param name="value">Value for the property.</param>
		/// <param name="propertyChanged">Property changed event handler to raise if the property value has changed.</param>
		/// <param name="propertyName">Name of the property, or omit to get the property name from the caller.</param>
		/// <typeparam name="T">The type of the property to set.</typeparam>
		public bool Set<T>(object key, T value, PropertyChangedEventHandler propertyChanged, [CallerMemberName] string propertyName = null)
		{
			var existing = Get<T>(key);
			if (!Equals(existing, value))
			{
				Set<T>(key, value);
				propertyChanged(Parent, new PropertyChangedEventArgs(propertyName));
				return true;
			}
			return false;
		}
	}
}
