# How to create and setup an SSL Cert

## pre-setup

- You need a domain name
- Set up cloud server: [How to set up google cloud server](https://mirror-networking.com/docs/Articles/Guides/DevServer/gcloud/index.html) 

> note: You may need to open port 80 for certbot

## Get Cert

Follows guides here:

https://letsencrypt.org/getting-started/
https://certbot.eff.org/instructions

Find the instructions for your server version, below is link for `Ubuntu 18.04 LTS (bionic)`

https://certbot.eff.org/lets-encrypt/ubuntubionic-other

For instruction 7

```
sudo certbot certonly --standalone
```

After filling in details you will get a result like this 

```
IMPORTANT NOTES:
 - Congratulations! Your certificate and chain have been saved at:
   /etc/letsencrypt/live/simpleweb.example.com/fullchain.pem
   Your key file has been saved at:
   /etc/letsencrypt/live/simpleweb.example.com/privkey.pem
   Your cert will expire on 2021-01-07. To obtain a new or tweaked
   version of this certificate in the future, simply run certbot
   again. To non-interactively renew *all* of your certificates, run
   "certbot renew"
 - If you like Certbot, please consider supporting our work by:

   Donating to ISRG / Let's Encrypt:   https://letsencrypt.org/donate
   Donating to EFF:                    https://eff.org/donate-le
```

`simpleweb.example.com` should be your domain

## Create cert.pfx

To create a pfx file that SimpleWebTransport can use run this command in the `/etc/letsencrypt/live/simpleweb.example.com/` folder

```sh
openssl pkcs12 -export -out cert.pfx -inkey privkey.pem -in cert.pem -certfile chain.pem
```

You might need to be super user in order to do this:

```
su

cd /etc/letsencrypt/live/simpleweb.example.com/
```

## Using cert.pfx

You can either copy the cert.pfx file to your server folder or create a symbolic link

Move
```sh
mv /etc/letsencrypt/live/simpleweb.example.com/cert.pfx ~/path/to/server/cert.pfx
```

Symbolic link
```sh
ln -s /etc/letsencrypt/live/simpleweb.example.com/cert.pfx ~/path/to/server/cert.pfx
```

### create cert.json file

Create a `cert.json` that SimpleWebTransport can read

Run this command in the `~/path/to/server/` folder

```sh
echo '{ "path":"./cert.pfx", "password": "" }' > cert.json
```


### Run your server

after the `cert.json` and `cert.pfx` are in the server folder you can start you using using
```
./demo.x86_64
```

> you may need to use `sudo` to run if you created a symbolic link

### Connect to your game 

Test everything is working by connection using the editor or a build 

set your domain (eg `simpleweb.example.com`) in the hostname field and then start a client
