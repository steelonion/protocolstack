# 虚拟协议栈

## 介绍

通过软件实现常用协议栈，便于在软件中模拟计算机节点

## 实现

- [x] ARP
- [x] ICMP/Ping
- [ ] ICMP
- [x] IP
- [x] TCP
- [x] UDP/Client
- [ ] UDP/Server

## 问题

- 目前测试下来似乎Windows下会拦截非网卡MAC地址的包，导致测试程序无法响应外部报文