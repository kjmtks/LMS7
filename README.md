# LMS7

## Quick start

Install docker and docker-compose.

Run following commands:

```
$ git clonet https://github.com/kjmtks/LMS7.git
$ cd LMS7
$ make development-up
```

then, open `http://localhost:8080` in your browser.

The initial user account and password are `admin` and `admin`, respectively.

### Uninstall

```
$ make development-remove
```

## Run in Production

Install docker and docker-compose.

Run following commands:

```
$ git clonet https://github.com/kjmtks/LMS7.git
$ cd LMS7
$ vim docker-compose.production.default.yaml
(or $ vim docker-compose.production.override.yaml)
$ make pfx KEY=your-key-file.key CER=your-cert-file.cer
$ cp your-ca-file.cer certs/
$ make production-up
```

then, open `https://localhost` (or `https://yourhost`) in your browser.

### Stop

```
$ make production-down
```

### Uninstall

```
$ make production-remove
```
