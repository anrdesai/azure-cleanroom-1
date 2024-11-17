#!/bin/bash
set -o errexit

cluster_name='cleanroom'
kind delete cluster --name $cluster_name