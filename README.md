# SimpleK8sWatch

.NET standard 2.1 library (C#) providing an easy to use watcher for kubernetes events.

## Why

Because I don't like the approach of handling kubernetes watches in the official kubernetes client library for c# (https://github.com/kubernetes-client/csharp).

## What

Just a rather simple wrapper around a kubernetes watch with the following features:
- Keeps an always-up-to-date cache of all entities received by a watcher. Cache can be retrieved synchronously without any delay and without accesses to the kubernetes API.
- Automatically attempts reconnect (every 10 seconds) when connection is lost
- Compares entities on event retrieval, only triggers a c# event if the entity really changed.
- Dependency injection friendly

## How

See SimpleK8sWatchExamples/Program.cs for a minimal example. Basically, you get a typed watcher 
`new WatchedResource<V1ConfigMap, V1ConfigMapList>`, you need to pass the requested entity type and the kubernetes type of a list of the same entities.

In the constructor, you need to pass a method from the kubernetes library that is used to initialize a watch to this entity type: `(doWatch, limit) => k8S.ListNamespacedConfigMapWithHttpMessagesAsync(ns, watch: doWatch, limit: limit), logger)`
SimpleK8sWatch will call this method in different ways, therefore you need to let SimpleK8sWatch know how to pass the required parameters (doWatch and limit).

You need a fully working "Kubernetes" object. How to properly initialize this object is different for a lot of kubernetes providers, but usually rather easy to find out.
The example code contains the most simple approach by just using the credentials from your local ".kube/config"-file. 

## Who

Tobias Braun, tobi.braun (at) gmail (dot) com

## What else

I've created this library for my personal use. Feel free to use it as you like, but don't take me responsible if it isn't doing what you expected...
