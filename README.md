# Yuh.Collections

## Overview

This library provides collection types including deque, and also provides methods to search through a collection.

## Features

### Aggressive Use of Span&lt;T&gt;

We actively use Span&lt;T&gt; or ReadOnlySpan&lt;T&gt; as a argument of methods or in internal implementations.
This enables functions to run faster and save memory.

We also provide collection types that can be converted to Span&lt;T&gt; or ReadOnlySpan&lt;T&gt;.

### Performance-oriented Implementation

We focus on making functions perform better.
We not only use Span&lt;T&gt;, but also use throwing-methods instead of throw-statements so that .NET Runtime can inline methods.

---

## 概要

両端キュー (deque) を含むコレクション型や, 探索に使う関数群を提供します.

## 特徴

### Span&lt;T&gt; を積極的に利用

Span&lt;T&gt; や ReadOnlySpan&lt;T&gt; をメソッドの引数や内部実装に積極的に採用し, 高速かつ余計なメモリ領域を占領しない動作を実現しています.

また, Span&lt;T&gt; や ReadOnlySpan&lt;T&gt; への変換に対応したコレクション型も提供しています.

### 徹底的にパフォーマンスを重視した実装

Span&lt;T&gt; の活用はもちろん, 例外のスローを別の関数に任せることでインライン化による高速化を図るなどしています.
