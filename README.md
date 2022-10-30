# Object Translator

This is a library for converting one C# object into another object using a simple domain-specific language (DSL). It's is designed to be used prior to serlization, so that a new objects can be created that will result in the desired serialization.

* **Source:** The object you have
* **Specification:** A set of instructions that explains how to convert Source into Target
* **Target:** The object you want (most likely because you want serialize it in a specific format)

So the specification is simple DSL that tells C# how to turn Source into Target. Most of the documentation below explains how to write this DSL.

## Usage

The C# is trivial:

```
var spec = [read in a string, explained below...]
var targetObject = ObjectTranslator.Translate(spec, sourceObject)
```
The Target will be an `ExpandoObject` which serializes very cleanly with `System.Text.Json.JsonSeralizer.Serialize`.

If you want, you can go straight to JSON with:

```
var json = ObjectTranslator.ToJson(spec, sourceObject)
```

(Why `ToJson`? Because I might add `ToXml` later.)

(I like XML. Fight me.)

(Oh, you wanna fight? ... then I'll add `ToYaml`. ... Yeah. F*** around and find out, punk.)

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

For simple collection, like `Pets`, we can copy over by simply using the name like other properties.

```
Pets
```

If we don't specify any children (see below), which will simply copy over the string representation of whatever the child is (for a lit of strings, which is fine).

But if we have a collection of objects, like `Children`, we can specify sub-items explaining what we want from each `Person` object in `Children`. If we want a simple list of their names and heights, for example, we can do this:

```
Children
  Name
  Height
```

Each one of the sub-items operates just like a top level item. We can do this:

```
Children
  Name
  Height
  Year: DateOfBirth.Year
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

Say we wanted a very simple output -- just a few properties. We could apply this:

```
first: Name.First
last: Name.Last
dob: DateOfBirth
```

That would give us:


```
first: "Taylor"
last: "Swift"
dob: "1989-12-13"
```

Or, if going straight to JSON:

```json
{
  "first": "Taylor",
  "last": "Swift",
  "dob": "1989-12-13T00:00:00"
}
```

Or, we could get *way* more complicated:

```
first_name: Name.First
last_name: Name.Last
legal_name: Name.Last | append:"," | append:NameFirst | upcase
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
legal_name: "SWIFT,TAYLOR"
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

## Limitations

Honestly, the parsing isn't great. It's a mess of procedural code and flags.

* I need to figure out the indent situation. What constitutes an indent? Right now, I'm counting spaces.
* One absolute limitation: you can't "recede" more than one level at a time. If you're indented to level 3, you can't jump back to level 1. Any reduction in the level of indent, will back you up ONE level only. I need to figure this out.

Also, there's little consideration of typing, and I'm not sure how much this matters. We're just serializing, so do we care about underlying types on Target? I'm not sure.

## A Final Word

Yes, yes, I get it -- why do this when it's easier to just use a programming language?

*Because this is simple, plain text, which can be edited in a UI, stored in a repository, and sandboxed during execution.*

That's it, nothing more. That's the whole reason.

So, don't come at me.
