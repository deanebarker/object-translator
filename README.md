# Object Translator

This is a library for converting one C# object into another object using a simple domain-specific language. It's is designed to be used prior to serlization, so that a new objects can be created that will result in desired serialization.

Assume a **Source** object and a **Target** object. The library will ready in the specification, apply it to the Source, and output the Target. It's then assumed that the target will be serialized by some other process.

## Usage

The C# is pretty simple:

```
var spec = [read in a string, explained below...]
var targetObject = ObjectTranslator.Translate(spec, sourceObject)
```
The Target will be an `ExpandoObject` which serializes very cleanly with `System.Text.Json.JsonSeralizer.Serialize`.

If you want, you can go straight to JSON with:

```
var json = ObjectTranslator.Serialize(spec, sourceObject)
```

## Specification Language

Key point: *nothing is assumed to go from Source to Target*. Every transfer from Source to Target has to be explicitly specified.

For the examples below, assume we have a source object that looks like this:

```
Person
---
Name (object)
  First (string)
  Last (string)
DateOfBirth (datetime)
Height (int)
Weight (int)
Children (List<Person>)
Pets (List<string>)
```

### Properties

If we just want to output the date of birth in a new object, our specification looks like this:

```
DateOfBirth
```

That will create Target with one property -- `DateOfBirth` -- that has the value from Source. All other properties on Source will be ignored.

If we want to change the property name, we can do this:

```
dob: DateOfBirth
```

That will do the same thing, but the property name on Target will now be `dob` with the same value.

We can continue to specify properties, one on each line:

```
dob: DateOfBirth
Height
Weight
```

Now Target has three properties: `dob`, `Height`, and `Weight` with the corresponding values from Source.

Say, we just want the year of birth. We can drill down into values like this:

```
year_of_birth: DateOfBirth.Year
```

That will resolve to the `Year` property of the `DateTime` object and produce the value.

### Collections

For simple collections of primitives, like `Pets`, we can copy over by simply using the name like other properties.

```
Pets
```

But note that `Children` is a list of `Person` objects. Just specifying `Children` would actually do nothing. An `ICollection` of non-primitives isn't anything by itself -- we would need to specify sub-items explaining what we want from each `Person` object in `Children`. If we want a simple list of their names and heights, for example, we can do this:

```
Children
  Name
  Height
```

### Fluid Expressions

We can also use Fluid expressions to output modified data. For example, if we want a formatted version of their date of birth, we can do this:

```
friendly_date: DateOfBirth | format_date:"MMMM d, yyyy"
```

Using expressions, we can also "invent" properties that don't exist, the values of which are the result of expressions:

```
number_of_children: Children | size
```

If we want to perform expressions on the values in a list of primitives and create a list on Target with a more complicated object, we have to use a special token -- `_` -- to represent the value itself.

```
Pets
  name: _
  length_of_name: _ | size
```

## Example

Using our object definition from above, consider this Source (forgive the object notation -- I think you'll get the point):

```
Name
  First: "Taylor"
  Last: "Swift"
DateOfBirth "1989-12-13"
Height: 71
Weight: 135
Children: [list of ex-boyfriends, because they're were all so damn childish...]
Pets: "Meredith", "Benjamin", "Olivia"
```

Now, we'll apply this specification (this is the actual text of the specification):

```
first_name: Name.First
last_name: Name.Last
dob: DateOfBirth
height_in_inches: Height
weight_in_pounds: Weight
weight_in_kilograms: Weight | divided_by:2.2 | floor
pets: Pets
  name: _
  relationship: "Taylor loves " | append:_
douchebag_count: Children | size
idiot_ex_boyfriends: Children
   name: Name.First | append:" " | append:Name.Last
   summary: Weight | append: " pounds of drama"
```

This results in this Target:


```
first_name: "Taylor"
last_name: "Swift"
dob: "1989-12-13"
height_in_inches: 71
weight_in_pounds: 135
weight_in_kilograms: 61
pets:
  - name: "Meredith"
    relationship: "Taylor loves Meredith"
  - name: "Benjamin"
    relationship: "Taylor loves Benjamin"
  - name: "Olivia"
    relationship: "Taylor loves Olivia"
douchebag_count: 3
idiot_ex_boyfriends:
  - name: "Jake G...no one knows how to spell this..."
    summary: "180 pounds of drama"
  - name: "Harry Styles"
    summary: "150 pounds of drama"
  - name: "Joe Jonas"
    summary: "160 pounds of drama"

```
