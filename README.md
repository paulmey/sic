# Sharing is caring
Sharing is caring is reservation system for shared resources (apartments, spaces, equipment, etc.).

## Features
- Registered users can reserve/book a resource for an arbitrary amount of time, can be multi-day down to the minute.
- The system prevents overlap between reservations.
- Users can see eachothers reservations for a space
- The system uses existing identity providers (microsoft personal accounts and google in v1) instead of storing credentials.
- The system is web based
- Works great on mobile
- Costs very little to run on a cloud platform
- Resources have a name, description, image and category. Categories have a name and an icon. There is no other hierarchy
- Bookings have a short title (30 chars) and a slightly longer description (~1k), both optional
- There is a calendar view and an agenda view, with filters for the resources
- Scales well from 5-1000 users
- Cheaply deployable on Azure using free tiers. Uses well-known tech stack

## User authorization setup 
- The first user to log in will become the first administrator
- After the administrator is established, users can only join using an (expiring) invite link
- Users will have a 'profile' to set their display name
- There will always be at least one administrator
- There is role based access control at the app level, and at the resource level:
    - App level:
        - User administrator (user CRUD, including app RBAC)
        - Category administrator (category CRUD)
        - Resource administrator (resource CRUD and resource RBAC)
    - Per resource:
        - User (CRUD their own bookings)
        - Manager (CRUD everyone's bookings for this resource)

## Not in scope for this version
- recurring bookings
- resource hierarchy
- custom resource attributes for filters
- booking rules per resource/user, such as:
    - min/max duration
    - max book ahead time
    - only past/future
- booking approval workflow
- notifications
- auto purging/cleanup
- multi-tenancy (multiple resource pools / user groups using the same system)
- resource deactivation (temporarily unavailable or 'invisible')
- bulk operations
- multi-timezone (backend stores everything in UTC, frontend translates to browser timezone)

## Possible applications
- Apartment sharing
- (Meeting) room booking
- Tool/equipment sharing
- Boat booking for rowing clubs