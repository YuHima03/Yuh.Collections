# Yuh.Collections

## Overview

This library provides collection types including deque, and also provides methods to search through a collection.

## Features

### Advanced Collection Types

#### Deque

Deque (double-ended-queue) supports addition or removal of elements at the front or back of collection in constant time.
In other words, deque has features of both stack and queue.

#### Collection Builder

Collection-builder is literally used to build collections such as array, list, string and so on.
Extension methods for `IEnumerable<T>` perform little better than the collection-builder in this library, but the collection-builder supports merging `IEnumerable<T>`s like `StringBuilder` class.

### Aggressive Use of Span&lt;T&gt;

We actively use Span&lt;T&gt; or ReadOnlySpan&lt;T&gt; as a argument of methods or in internal implementations.
This enables functions to run faster and save memory.

We also provide collection types that can be converted to Span&lt;T&gt; or ReadOnlySpan&lt;T&gt;.

## How to Install

To install this library into your projects, please visit [here](https://www.nuget.org/packages/Yuh.Collections) (NuGet) and install the NuGet package in the way you like.
