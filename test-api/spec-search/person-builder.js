class Name {
    constructor() {
        this.firstName = "";
        this.lastName = "";
    }

}

class ContactDetails {
    constructor() {
        this.phone = [];
        this.email = [];
    }
}

class Coord {
    constructor(lat, lon) {
        this.lat = lat;
        this.lon = lon;
    }
}

class Geo {
    constructor() {
        this.coord = null;
    }
}

class Address {
    constructor() {
        this.region = null;
        this.geo = null;
    }
}

class Person {
    constructor(id ) {
        this.id = id;
        this.name = new Name();
        this.tags = [];
        this.poolStatuses = [];
        this.contactDetails = new ContactDetails();
        this.address = null;
    }
}

module.exports = {
    Person: Person,
    Address: Address
}