using System.Collections.Specialized;
using ComApi = Autodesk.Navisworks.Api.Interop.ComApi;

namespace BulkProps.Process
{
    public abstract class PropertyProcessor
    {
        public string PropertyName;

        // `UserName`, for display to the end user, and a fixed `Name` for use in programming.
        // The value can be one of five types:
        //Double precision floating point
        //32 bit integer
        //Boolean
        //Wide string
        //Date / Time
        public string Value;
        public static bool UsesValue { get; }
        public PropertyProcessor() { }
        public PropertyProcessor(string name)
        {
            PropertyName = name;
        }
        public abstract BulkPropsException Process(ProcessContext context, OrderedDictionary propsForTab);
    }

    public abstract class PropertyProcessorWithValue : PropertyProcessor
    {
        public static new bool UsesValue { get; } = true;
        public PropertyProcessorWithValue() : base() { }
        public PropertyProcessorWithValue(string name, string value) : base(name)
        {
            Value = value;
        }
    }

    public class UpsertPropertyProcessor : PropertyProcessorWithValue
    {
        public UpsertPropertyProcessor() : base() { }
        public UpsertPropertyProcessor(string name, string value) : base(name, value) { }
        public override BulkPropsException Process(ProcessContext context, OrderedDictionary propsForTab)
        {
            // confirm the conditions are correct for doing this process
            // IDEA should there be logging for when the process doesn't apply?
            if (!confirmProcess(propsForTab)) return null;

            // create new property
            ComApi.InwOaProperty newProp = context.ComState.ObjectFactory(ComApi.nwEObjectType.eObjectType_nwOaProperty, null, null) as ComApi.InwOaProperty;

            // set the names of the new property
            (string displayName, string internalName, BulkPropsException err) = context.ProcessPropertyName(PropertyName);
            if (err != null)
            {
                return err;
            }
            newProp.name = internalName;
            newProp.UserName = displayName;

            // set the value of the new property
            string formattedValue;
            (formattedValue, err) = context.ProcessPropertyValue(Value);
            if (err != null)
            {
                return err;
            }
            newProp.value = formattedValue;

            // add the new property to the dictionary
            propsForTab[internalName] = newProp;

            return null;
        }

        public virtual bool confirmProcess(OrderedDictionary props)
        {
            // always confirm for upsert
            return true;
        }
    }

    public class InsertPropertyProcessor : UpsertPropertyProcessor
    {
        public InsertPropertyProcessor() : base() { }
        public InsertPropertyProcessor(string name, string value) : base(name, value) { }
        public override bool confirmProcess(OrderedDictionary props)
        {
            // ensure the property does not exist
            return props.Contains(PropertyName) == false;
        }
    }

    public class UpdatePropertyProcessor : UpsertPropertyProcessor
    {
        public UpdatePropertyProcessor() : base() { }
        public UpdatePropertyProcessor(string name, string value) : base(name, value) { }
        public override bool confirmProcess(OrderedDictionary props)
        {
            // ensure the property does exist
            return props.Contains(PropertyName) == true;
        }
    }

    public class DeletePropertyProcessor : PropertyProcessor
    {
        public static new bool UsesValue { get; } = false;
        public DeletePropertyProcessor() : base() { }
        public DeletePropertyProcessor(string name) : base(name) { }
        public override BulkPropsException Process(ProcessContext context, OrderedDictionary propsForTab)
        {
            (string displayName, string internalName, BulkPropsException err) = context.ProcessPropertyName(PropertyName);
            if (err != null)
            {
                return err;
            }
            // IDEA should there be logging for when the process doesn't apply?
            // ie on `!propsForTab.Has(internalName)`
            propsForTab.Remove(internalName);
            return null;
        }
    }

}
